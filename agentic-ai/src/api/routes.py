"""Coordinator API routes."""

import logging
from uuid import UUID

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field

from src.coordinator_agent.execution import (
    CoordinatorExecutionError,
    CoordinatorProviderError,
    CoordinatorValidationError,
    execute_coordinator_agent,
)
from src.coordinator_agent.models import CoordinatorPlanOutput

router = APIRouter(prefix="/api/coordinator", tags=["Coordinator"])
logger = logging.getLogger(__name__)


class GenerateCoordinatorPlanRequest(BaseModel):
    """Backend request for generating a coordinator plan."""

    event_id: UUID = Field(..., alias="eventId")
    event: dict[str, object]

    model_config = {"populate_by_name": True}


@router.get("/schema", response_model=dict[str, object])
async def coordinator_schema() -> dict[str, object]:
    """Expose the structured output schema for integration clients."""

    return CoordinatorPlanOutput.get_json_schema_for_gemini()


@router.post("/generate", response_model=CoordinatorPlanOutput)
async def generate_coordinator_plan(
    request: GenerateCoordinatorPlanRequest,
) -> CoordinatorPlanOutput:
    """Generate a validated plan for the backend integration."""

    try:
        return await execute_coordinator_agent(str(request.event_id), request.event)
    except TimeoutError as exc:
        logger.warning("Coordinator request timed out for event %s", request.event_id)
        raise HTTPException(
            status_code=504,
            detail="Plan generation took too long. Please try again.",
        ) from exc
    except CoordinatorProviderError as exc:
        logger.error("Gemini provider failed for event %s", request.event_id)
        raise HTTPException(
            status_code=502,
            detail="The AI provider could not generate a plan. Please try again later.",
        ) from exc
    except CoordinatorValidationError as exc:
        logger.warning("Generated plan failed coordinator validation for event %s", request.event_id)
        raise HTTPException(
            status_code=422,
            detail="The generated plan did not pass validation. Please try again.",
        ) from exc
    except CoordinatorExecutionError as exc:
        logger.exception("Coordinator plan generation failed for event %s", request.event_id)
        raise HTTPException(
            status_code=500,
            detail="Plan generation failed. Please try again later.",
        ) from exc
