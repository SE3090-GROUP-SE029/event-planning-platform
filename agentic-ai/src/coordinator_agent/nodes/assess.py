"""Budget-allocation node."""

import json
import logging

from pydantic import BaseModel, Field

from src.coordinator_agent.prompts import BUDGET_ALLOCATION_PROMPT
from src.gemini_client.client import GeminiClient
from ..state import CoordinatorState

logger = logging.getLogger(__name__)


class BudgetOutput(BaseModel):
    allocation: dict[str, float] = Field(default_factory=dict)

def _rebalance(allocation: dict[str, float], budget: float) -> dict[str, float]:
    positive = {key: value for key, value in allocation.items() if value > 0}
    if not positive or budget <= 0:
        return {}
    total = sum(positive.values())
    factor = budget / total if total > budget else 1.0
    return {key: round(value * factor, 2) for key, value in positive.items()}


async def allocate_budget(state: CoordinatorState) -> dict[str, dict[str, float]]:
    """Allocate a positive, budget-bounded amount to every service category."""

    budget = float(state.event.get("budget", 0) or 0)
    prompt = BUDGET_ALLOCATION_PROMPT.format(
        total_budget=budget,
        service_categories=json.dumps(state.service_categories),
        guest_count=state.event.get("guest_count", "unknown"),
    )
    result = await GeminiClient().generate_with_prompt(prompt, BudgetOutput)
    allocation = {
        category: float(result.allocation.get(category, 0))
        for category in state.service_categories
    }
    allocation["Contingency"] = float(result.allocation.get("Contingency", 0))
    allocation = _rebalance(allocation, budget)
    if any(value <= 0 for value in allocation.values()):
        raise ValueError("budget allocation must assign a positive amount to every category")
    return {"budget_allocation": allocation}
