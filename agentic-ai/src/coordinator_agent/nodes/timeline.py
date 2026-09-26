"""High-level planning timeline node."""

from datetime import date, datetime

from pydantic import BaseModel, Field

from src.coordinator_agent.prompts import TIMELINE_GENERATION_PROMPT
from src.gemini_client.client import GeminiClient
from ..state import CoordinatorState


class TimelinePhase(BaseModel):
    phase_name: str = Field(..., min_length=1)
    timing: str = Field(..., min_length=1)


class TimelineOutput(BaseModel):
    phases: list[TimelinePhase] = Field(..., min_length=3, max_length=8)


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
    result = await GeminiClient().generate_with_prompt(
        prompt, TimelineOutput, node_name="propose_timeline"
    )
    phases: dict[str, str] = {}
    for phase in result.phases:
        name = phase.phase_name.strip()
        timing = phase.timing.strip()
        if not name or not timing:
            continue
        if name in phases:
            raise ValueError(f"timeline contains duplicate phase name: {name}")
        phases[name] = timing
    if len(phases) < 3:
        raise ValueError("timeline must contain at least three non-empty phases")
    return {"proposed_timeline": dict(list(phases.items())[:8])}
