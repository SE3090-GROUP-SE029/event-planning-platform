"""Coordinator API routes."""

from fastapi import APIRouter

from src.coordinator_agent.models import CoordinatorPlanOutput

router = APIRouter(prefix="/api/coordinator", tags=["Coordinator"])


@router.get("/schema", response_model=dict[str, object])
async def coordinator_schema() -> dict[str, object]:
    """Expose the structured output schema for integration clients."""

    return CoordinatorPlanOutput.get_json_schema_for_gemini()
