"""Coordinator API routes."""

import logging
from uuid import UUID

from fastapi import APIRouter, HTTPException, Request
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
_PROVIDER_MESSAGES = {
    "quota_exhausted": "Gemini quota is exhausted. Retry later.",
    "rate_limit": "Gemini is rate limiting requests. Retry later.",
    "network_issue": "Gemini is temporarily unavailable.",
    "invalid_model": "No configured Gemini model can serve this request.",
    "invalid_credentials": "Gemini credentials are invalid or expired.",
}


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
    http_request: Request,
) -> CoordinatorPlanOutput:
    """Generate a validated plan for the backend integration."""

    try:
        return await execute_coordinator_agent(
            str(request.event_id),
            request.event,
            checkpointer=getattr(
                http_request.app.state, "coordinator_checkpointer", None
            ),
        )
    except TimeoutError as exc:
        logger.warning("Coordinator request timed out for event %s", request.event_id)
        raise HTTPException(
            status_code=504,
            detail={
                "code": "provider_timeout",
                "message": "Plan generation took too long. Please try again.",
            },
        ) from exc
    except CoordinatorProviderError as exc:
        logger.error(
            "Gemini provider failed for event %s category=%s",
            request.event_id,
            exc.code,
        )
        raise HTTPException(
            status_code=exc.status_code,
            detail={
                "code": exc.code,
                "message": _PROVIDER_MESSAGES.get(
                    exc.code,
                    "The AI provider could not generate a plan. Please try again later.",
                ),
            },
            headers=(
                {"Retry-After": str(exc.retry_after)}
                if exc.retry_after is not None
                else None
            ),
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
