"""Async entry point for executing the coordinator graph."""

import asyncio
import logging
from typing import Any
from uuid import UUID

from pydantic import ValidationError

from src.coordinator_agent.config import get_settings
from src.gemini_client.exceptions import GeminiClientError, GeminiTimeoutError

from .graph import build_coordinator_graph
from .models import CoordinatorPlanOutput
from .state import CoordinatorState

logger = logging.getLogger(__name__)


class CoordinatorExecutionError(RuntimeError):
    """Raised when coordinator execution fails."""


class CoordinatorProviderError(CoordinatorExecutionError):
    """Raised when the upstream Gemini provider cannot fulfill a request."""


class CoordinatorValidationError(CoordinatorExecutionError):
    """Raised when the generated plan does not satisfy coordinator validation."""


async def _run_graph_async(graph: Any, initial_state: CoordinatorState) -> CoordinatorState:
    result = await graph.ainvoke(initial_state)
    return CoordinatorState.model_validate(result)


async def execute_coordinator_agent(
    event_id: str,
    event_data: dict[str, Any],
    max_retries: int | None = None,
    timeout_seconds: int | None = None,
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
        logger.info("Starting coordinator agent for event %s", event_id)
        final_state = await asyncio.wait_for(
            _run_graph_async(build_coordinator_graph(), initial_state),
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
        return final_state.to_coordinator_plan_response()
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
