"""Risk-assessment node."""

import json
import logging

from pydantic import BaseModel, Field

from src.coordinator_agent.models import RiskModel
from src.coordinator_agent.prompts import RISK_ASSESSMENT_PROMPT
from src.gemini_client.client import GeminiClient
from ..state import CoordinatorState

logger = logging.getLogger(__name__)


class RisksOutput(BaseModel):
    risks: list[RiskModel] = Field(default_factory=list, max_length=20)

SEVERITY_ORDER = {"High": 3, "Medium": 2, "Low": 1}


async def assess_risks(state: CoordinatorState) -> dict[str, list[RiskModel]]:
    """Assess and rank planning-relevant risks."""

    prompt = RISK_ASSESSMENT_PROMPT.format(
        event_details=json.dumps(state.event, ensure_ascii=True, default=str),
        budget=json.dumps(state.budget_allocation),
        constraints=state.requirements_analysis,
    )
    result = await GeminiClient().generate_with_prompt(prompt, RisksOutput)
    unique: dict[str, RiskModel] = {}
    for risk in result.risks:
        key = risk.risk.strip().casefold()
        if key and risk.recommendation.strip():
            unique[key] = risk
    risks = sorted(
        unique.values(), key=lambda item: SEVERITY_ORDER[item.severity], reverse=True
    )
    return {"identified_risks": risks[:7]}
