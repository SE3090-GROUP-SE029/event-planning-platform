"""Async entry point for executing the coordinator graph."""

import asyncio
import hashlib
import json
import logging
import threading
from typing import Any
from uuid import UUID
from weakref import WeakValueDictionary

from langgraph.checkpoint.base import BaseCheckpointSaver
from pydantic import ValidationError

from src.coordinator_agent.config import get_settings
from src.gemini_client.exceptions import (
    GeminiClientError,
    GeminiInvalidCredentialsError,
    GeminiInvalidModelError,
    GeminiNetworkError,
    GeminiQuotaError,
    GeminiRateLimitError,
    GeminiTimeoutError,
)

from .graph import build_coordinator_graph
from .models import CoordinatorPlanOutput
from .state import CoordinatorState

logger = logging.getLogger(__name__)
_execution_locks: WeakValueDictionary[tuple[int, str], asyncio.Lock] = WeakValueDictionary()
_execution_locks_guard = threading.Lock()
_WORKFLOW_CHECKPOINT_VERSION = "v1"


class CoordinatorExecutionError(RuntimeError):
    """Raised when coordinator execution fails."""


class CoordinatorProviderError(CoordinatorExecutionError):
    """Raised when the upstream Gemini provider cannot fulfill a request."""

    def __init__(
        self,
        message: str,
        code: str = "provider_error",
        status_code: int = 502,
        retry_after: int | None = None,
    ) -> None:
        super().__init__(message)
        self.code = code
        self.status_code = status_code
        self.retry_after = retry_after


class CoordinatorValidationError(CoordinatorExecutionError):
    """Raised when the generated plan does not satisfy coordinator validation."""


def _execution_lock(thread_id: str) -> asyncio.Lock:
    loop = asyncio.get_running_loop()
    key = (id(loop), thread_id)
    with _execution_locks_guard:
        lock = _execution_locks.get(key)
        if lock is None:
            lock = asyncio.Lock()
            _execution_locks[key] = lock
        return lock


async def _run_graph_async(
    graph: Any,
    initial_state: CoordinatorState,
    config: dict[str, Any],
    checkpointer: BaseCheckpointSaver | None,
) -> CoordinatorState:
    if checkpointer is None:
        result = await graph.ainvoke(initial_state)
    else:
        checkpoint = await graph.aget_state(config)
        if checkpoint.values.get("final_plan") is not None:
            logger.info(
                "Coordinator completed plan restored from checkpoint event=%s",
                initial_state.event_id,
            )
            return CoordinatorState.model_validate(checkpoint.values)
        if checkpoint.values:
            logger.info(
                "Resuming coordinator from checkpoint event=%s next_nodes=%s",
                initial_state.event_id,
                checkpoint.next,
            )
            result = await graph.ainvoke(None, config)
        else:
            result = await graph.ainvoke(initial_state, config)
    return CoordinatorState.model_validate(result)


async def execute_coordinator_agent(
    event_id: str,
    event_data: dict[str, Any],
    max_retries: int | None = None,
    timeout_seconds: int | None = None,
    checkpointer: BaseCheckpointSaver | None = None,
) -> CoordinatorPlanOutput:
    """Execute the coordinator graph with timeout and final-state validation."""

    settings = get_settings()
    iteration_limit = max_retries if max_retries is not None else settings.max_iterations
    execution_timeout = (
        timeout_seconds if timeout_seconds is not None else settings.coordinator_timeout_seconds
    )
    try:
        parsed_event_id = UUID(event_id)
        initial_state = CoordinatorState(
            event_id=parsed_event_id,
            event=event_data,
            max_iterations=iteration_limit,
        )
        event_fingerprint = hashlib.sha256(
            json.dumps(
                event_data,
                ensure_ascii=False,
                sort_keys=True,
                default=str,
            ).encode("utf-8")
        ).hexdigest()
        thread_id = (
            f"{parsed_event_id.hex}:{_WORKFLOW_CHECKPOINT_VERSION}:{event_fingerprint}"
        )
        config: dict[str, Any] = {"configurable": {"thread_id": thread_id}}
        logger.info("Starting coordinator agent for event %s", event_id)
        async with _execution_lock(thread_id):
            final_state = await asyncio.wait_for(
                _run_graph_async(
                    build_coordinator_graph(checkpointer),
                    initial_state,
                    config,
                    checkpointer,
                ),
                timeout=execution_timeout,
            )
        if not final_state.validation_passed or final_state.final_plan is None:
            raise ValidationError.from_exception_data(
                "CoordinatorState",
                [
                    {
                        "type": "value_error",
                        "loc": ("validation",),
                        "input": final_state.validation_errors,
                        "ctx": {"error": "final coordinator validation failed"},
                    }
                ],
            )
        logger.info("Coordinator agent completed for event %s", event_id)
        if checkpointer is not None:
            await checkpointer.adelete_thread(thread_id)
        return final_state.to_coordinator_plan_response()
    except GeminiQuotaError as exc:
        logger.warning("Gemini quota exhausted for event %s", event_id)
        raise CoordinatorProviderError(
            "The AI provider quota is exhausted. Retry this request later.",
            code="quota_exhausted",
            status_code=429,
            retry_after=60,
        ) from exc
    except GeminiRateLimitError as exc:
        logger.warning("Gemini rate limit reached for event %s", event_id)
        raise CoordinatorProviderError(
            "The AI provider is rate limiting requests. Retry this request later.",
            code="rate_limit",
            status_code=429,
            retry_after=30,
        ) from exc
    except GeminiNetworkError as exc:
        logger.warning("Gemini network/provider outage for event %s", event_id)
        raise CoordinatorProviderError(
            "The AI provider is temporarily unavailable.",
            code="network_issue",
            status_code=503,
            retry_after=15,
        ) from exc
    except GeminiInvalidModelError as exc:
        logger.error("No configured Gemini model is available for event %s", event_id)
        raise CoordinatorProviderError(
            "No configured AI model is available for this request.",
            code="invalid_model",
            status_code=502,
        ) from exc
    except GeminiInvalidCredentialsError as exc:
        logger.error("All configured Gemini credentials were rejected")
        raise CoordinatorProviderError(
            "The AI provider credentials are invalid or expired.",
            code="invalid_credentials",
            status_code=503,
        ) from exc
    except GeminiTimeoutError as exc:
        logger.error("Gemini provider timed out for event %s", event_id)
        raise TimeoutError("Plan generation timed out while waiting for Gemini") from exc
    except asyncio.TimeoutError as exc:
        logger.error("Coordinator agent timed out for event %s", event_id)
        raise TimeoutError(
            f"Plan generation exceeded {execution_timeout} second timeout"
        ) from exc
    except ValidationError as exc:
        logger.error("Coordinator final validation failed for event %s", event_id)
        raise CoordinatorValidationError("Generated plan did not pass validation") from exc
    except CoordinatorExecutionError:
        raise
    except GeminiClientError as exc:
        logger.error("Gemini provider failed for event %s: %s", event_id, type(exc).__name__)
        raise CoordinatorProviderError("The AI provider could not generate a plan") from exc
    except Exception as exc:
        logger.exception("Coordinator agent failed for event %s", event_id)
        raise CoordinatorExecutionError(f"Plan generation failed: {exc}") from exc
