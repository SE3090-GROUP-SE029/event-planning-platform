"""Missing-requirements detection node."""

import json

from pydantic import BaseModel, Field

from src.coordinator_agent.models import MissingRequirementModel
from src.coordinator_agent.prompts import MISSING_REQUIREMENT_DETECTION_PROMPT
from src.gemini_client.client import GeminiClient
from ..state import CoordinatorState


class MissingRequirementsOutput(BaseModel):
    requirements: list[MissingRequirementModel] = Field(default_factory=list, max_length=20)

async def detect_missing_requirements(
    state: CoordinatorState,
) -> dict[str, list[MissingRequirementModel]]:
    """Detect and deduplicate planning-relevant information gaps."""

    provided = state.event.get("requirements", [])
    prompt = MISSING_REQUIREMENT_DETECTION_PROMPT.format(
        event_details=json.dumps(state.event, ensure_ascii=True, default=str),
        requirements=json.dumps(provided, ensure_ascii=True, default=str),
    )
    result = await GeminiClient().generate_with_prompt(prompt, MissingRequirementsOutput)
    present = json.dumps(state.event, ensure_ascii=True, default=str).casefold()
    unique: dict[str, MissingRequirementModel] = {}
    for item in result.requirements:
        key = item.requirement.strip().casefold()
        if key and key not in present:
            unique[key] = item
    return {"missing_requirements": list(unique.values())[:6]}
