"""Async entry point for executing the coordinator graph."""

import asyncio
import logging
from typing import Any
from uuid import UUID

from pydantic import ValidationError

from .graph import build_coordinator_graph
from .models import CoordinatorPlanOutput
from .state import CoordinatorState

logger = logging.getLogger(__name__)


class CoordinatorExecutionError(RuntimeError):
    """Raised when coordinator execution fails."""


async def _run_graph_async(graph: Any, initial_state: CoordinatorState) -> CoordinatorState:
    result = await graph.ainvoke(initial_state)
    return CoordinatorState.model_validate(result)


async def execute_coordinator_agent(
    event_id: str,
    event_data: dict[str, Any],
    max_retries: int = 2,
    timeout_seconds: int = 300,
) -> CoordinatorPlanOutput:
    """Execute the coordinator graph with timeout and final-state validation."""

    try:
        parsed_event_id = UUID(event_id)
        initial_state = CoordinatorState(
            event_id=parsed_event_id,
            event=event_data,
            max_iterations=max_retries,
        )
        logger.info("Starting coordinator agent for event %s", event_id)
        final_state = await asyncio.wait_for(
            _run_graph_async(build_coordinator_graph(), initial_state),
            timeout=timeout_seconds,
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
    except asyncio.TimeoutError as exc:
        logger.error("Coordinator agent timed out for event %s", event_id)
        raise TimeoutError(
            f"Plan generation exceeded {timeout_seconds} second timeout"
        ) from exc
    except ValidationError as exc:
        logger.error("Coordinator final validation failed for event %s", event_id)
        raise CoordinatorExecutionError(f"Plan validation failed: {exc}") from exc
    except CoordinatorExecutionError:
        raise
    except Exception as exc:
        logger.exception("Coordinator agent failed for event %s", event_id)
        raise CoordinatorExecutionError(f"Plan generation failed: {exc}") from exc
