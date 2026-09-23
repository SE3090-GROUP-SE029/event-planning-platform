"""High-level planning timeline node."""

import json
from datetime import date, datetime

from pydantic import BaseModel, Field

from src.coordinator_agent.prompts import TIMELINE_GENERATION_PROMPT
from src.gemini_client.client import GeminiClient
from ..state import CoordinatorState


class TimelineOutput(BaseModel):
    phases: dict[str, str] = Field(..., min_length=3, max_length=8)


def _complexity(state: CoordinatorState) -> str:
    guests = int(state.event.get("guest_count", 0) or 0)
    return "high" if guests >= 250 or len(state.service_categories) >= 6 else "standard"


async def propose_timeline(state: CoordinatorState) -> dict[str, dict[str, str]]:
    """Generate and normalize three to eight relative planning phases."""

    event_date = state.event.get("date", "unknown")
    if isinstance(event_date, (date, datetime)):
        event_date = event_date.isoformat()
    prompt = TIMELINE_GENERATION_PROMPT.format(
        event_date=event_date,
        complexity=_complexity(state),
        vendor_count=len(state.target_vendor_types),
    )
    result = await GeminiClient().generate_with_prompt(prompt, TimelineOutput)
    phases = {
        name.strip(): timing.strip()
        for name, timing in result.phases.items()
        if name.strip() and timing.strip()
    }
    if len(phases) < 3:
        raise ValueError("timeline must contain at least three non-empty phases")
    return {"proposed_timeline": dict(list(phases.items())[:8])}
