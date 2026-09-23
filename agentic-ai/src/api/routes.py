"""Coordinator API routes."""

from fastapi import APIRouter
from pydantic import BaseModel, Field

from src.coordinator_agent.execution import execute_coordinator_agent
from src.coordinator_agent.models import CoordinatorPlanOutput

router = APIRouter(prefix="/api/coordinator", tags=["Coordinator"])


class GenerateCoordinatorPlanRequest(BaseModel):
    """Backend request for generating a coordinator plan."""

    event_id: str = Field(..., alias="eventId")
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

    return await execute_coordinator_agent(request.event_id, request.event)
