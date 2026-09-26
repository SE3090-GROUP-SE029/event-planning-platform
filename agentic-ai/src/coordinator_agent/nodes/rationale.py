"""Planning-rationale generation node."""

import json

from pydantic import BaseModel, Field

from src.coordinator_agent.prompts import RATIONALE_GENERATION_PROMPT
from src.gemini_client.client import GeminiClient
from ..state import CoordinatorState


class RationaleOutput(BaseModel):
    rationale: str = Field(..., min_length=1, max_length=2000)


async def generate_rationale(state: CoordinatorState) -> dict[str, str]:
    """Generate a concise rationale grounded in the current plan data."""

    prompt = RATIONALE_GENERATION_PROMPT.format(
        event_details=json.dumps(state.event, ensure_ascii=False, default=str),
        service_categories=json.dumps(state.service_categories),
        budget_allocation=json.dumps(state.budget_allocation),
        timeline=json.dumps(state.proposed_timeline),
    )
    result = await GeminiClient().generate_with_prompt(
        prompt, RationaleOutput, node_name="generate_rationale"
    )
    rationale = result.rationale.strip()
    if len(rationale) < 100:
        raise ValueError("rationale must be substantive and at least 100 characters")
    return {"rationale": rationale[:2000]}
