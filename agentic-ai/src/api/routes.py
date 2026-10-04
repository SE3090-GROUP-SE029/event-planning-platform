"""Coordinator API routes."""

import logging
from datetime import datetime
from typing import Literal
from uuid import UUID

from fastapi import APIRouter, HTTPException, Request
from pydantic import BaseModel, ConfigDict, Field, field_validator

from src.agents.scheduling_agent import SchedulingAgent
from src.coordinator_agent.execution import (
    CoordinatorExecutionError,
    CoordinatorProviderError,
    CoordinatorValidationError,
    execute_coordinator_agent,
)
from src.coordinator_agent.models import CoordinatorPlanOutput
from src.gemini_client.client import GeminiClient
from src.gemini_client.exceptions import GeminiClientError
from src.models.scheduling_models import (
    GenerateScheduleRequest,
    GenerateScheduleResponse,
    ScheduleState,
)

router = APIRouter(prefix="/api/coordinator", tags=["Coordinator"])
schedule_router = APIRouter(prefix="/api/schedules", tags=["Scheduling"])
logger = logging.getLogger(__name__)
_PROVIDER_MESSAGES = {
    "quota_exhausted": "Gemini quota is exhausted. Retry later.",
    "rate_limit": "Gemini is rate limiting requests. Retry later.",
    "network_issue": "Gemini is temporarily unavailable.",
    "invalid_model": "No configured Gemini model can serve this request.",
    "invalid_credentials": "Gemini credentials are invalid or expired.",
}


class CoordinatorEventRequest(BaseModel):
    """Validated event payload sent by the backend planning client."""

    model_config = ConfigDict(extra="forbid", strict=True, allow_inf_nan=False)

    name: str = Field(..., min_length=1, max_length=500)
    type: Literal["WEDDING", "CORPORATE", "BIRTHDAY"]
    date: datetime
    location: str | None = Field(..., max_length=500)
    guest_count: int = Field(..., ge=1)
    budget: float = Field(..., gt=0, allow_inf_nan=False)
    requirements: str | None = Field(..., max_length=4000)

    @field_validator("name")
    @classmethod
    def reject_blank_name(cls, value: str) -> str:
        if not value.strip():
            raise ValueError("name must not be blank")
        return value

    @field_validator("date", mode="before")
    @classmethod
    def parse_backend_datetime(cls, value: object) -> datetime:
        if not isinstance(value, str) or "T" not in value:
            raise ValueError("date must be an ISO 8601 datetime string")
        try:
            return datetime.fromisoformat(value.replace("Z", "+00:00"))
        except ValueError as exc:
            raise ValueError("date must be an ISO 8601 datetime string") from exc


class GenerateCoordinatorPlanRequest(BaseModel):
    """Backend request for generating a coordinator plan."""

    model_config = ConfigDict(extra="forbid", strict=True, populate_by_name=True)

    event_id: UUID = Field(..., alias="eventId")
    event: CoordinatorEventRequest

    @field_validator("event_id", mode="before")
    @classmethod
    def parse_event_id(cls, value: object) -> UUID:
        if not isinstance(value, str):
            raise ValueError("eventId must be a UUID string")
        try:
            return UUID(value)
        except ValueError as exc:
            raise ValueError("eventId must be a valid UUID") from exc


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
            request.event.model_dump(mode="json"),
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


@schedule_router.post(
    "/generate",
    response_model=GenerateScheduleResponse,
    status_code=200,
    tags=["Scheduling"],
)
async def generate_schedule(
    request: GenerateScheduleRequest,
    http_request: Request,
) -> GenerateScheduleResponse:
    """Generate a structured event schedule from the backend request payload."""

    gemini_client = getattr(http_request.app.state, "gemini_client", None) or GeminiClient()
    agent = SchedulingAgent(gemini_client)

    state = ScheduleState(
        event_id=request.event_id,
        title=request.title,
        event_description=request.event_description or request.requirements,
        event_type=request.event_type,
        date=request.date,
        start_time=request.start_time,
        end_time=request.end_time,
        guest_count=request.guest_count,
        requirements=request.requirements,
        vendor_service_context=request.vendor_service_context,
    )

    try:
        return await agent.run(state)
    except TimeoutError as exc:
        logger.warning("Schedule generation timed out for event %s", request.event_id)
        raise HTTPException(
            status_code=504,
            detail={
                "code": "provider_timeout",
                "message": "Schedule generation took too long. Please try again.",
            },
        ) from exc
    except GeminiClientError as exc:
        logger.warning(
            "Schedule generation provider unavailable for event %s error=%s",
            request.event_id,
            type(exc).__name__,
        )
        raise HTTPException(
            status_code=503,
            detail="AI schedule generation is temporarily unavailable. Please try again later.",
        ) from exc
    except Exception as exc:
        logger.exception("Schedule generation failed for event %s", request.event_id)
        raise HTTPException(
            status_code=500,
            detail={
                "code": "schedule_generation_failed",
                "message": "Schedule generation failed. Please try again later.",
            },
        ) from exc
