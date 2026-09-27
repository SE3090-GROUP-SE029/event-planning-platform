"""Budget-allocation node."""

import json
import logging
import math
from decimal import Decimal, ROUND_HALF_UP

from pydantic import BaseModel, Field

from src.coordinator_agent.prompts import BUDGET_ALLOCATION_PROMPT
from src.gemini_client.client import GeminiClient
from ..state import CoordinatorState
from ..utils import normalize_service_category

logger = logging.getLogger(__name__)


class BudgetAllocationItem(BaseModel):
    category: str = Field(..., min_length=1)
    amount: float = Field(..., gt=0)


class BudgetOutput(BaseModel):
    allocation: list[BudgetAllocationItem] = Field(default_factory=list)


def _rebalance(allocation: dict[str, float], budget: float) -> dict[str, float]:
    if not math.isfinite(budget) or budget <= 0:
        raise ValueError("event budget must be a finite positive amount")

    positive = {key: value for key, value in allocation.items() if value > 0}
    if not positive:
        raise ValueError("budget allocation must contain positive amounts")

    decimal_budget = Decimal(str(budget)).quantize(
        Decimal("0.01"), rounding=ROUND_HALF_UP
    )
    total = sum(Decimal(str(value)) for value in positive.values())
    remaining = decimal_budget
    rebalanced: dict[str, float] = {}
    items = list(positive.items())
    for index, (category, amount) in enumerate(items):
        if index == len(items) - 1:
            adjusted = remaining
        else:
            adjusted = (decimal_budget * Decimal(str(amount)) / total).quantize(
                Decimal("0.01"), rounding=ROUND_HALF_UP
            )
            remaining -= adjusted
        if adjusted <= 0:
            raise ValueError(
                f"budget is too small to allocate a positive amount to {category}"
            )
        rebalanced[category] = float(adjusted)

    return rebalanced


async def allocate_budget(state: CoordinatorState) -> dict[str, dict[str, float]]:
    """Allocate a positive, budget-bounded amount to every service category."""

    budget = float(state.event.get("budget", 0) or 0)
    if not math.isfinite(budget) or budget <= 0:
        raise ValueError("event budget must be a finite positive amount")
    prompt = BUDGET_ALLOCATION_PROMPT.format(
        total_budget=budget,
        service_categories=json.dumps(state.service_categories),
        guest_count=state.event.get("guest_count", "unknown"),
    )
    result = await GeminiClient().generate_with_prompt(
        prompt, BudgetOutput, node_name="allocate_budget"
    )
    categories_by_normalized_name = {
        normalize_service_category(category): category
        for category in state.service_categories
    }
    categories_by_normalized_name["contingency"] = "Contingency"
    requested_allocations: dict[str, float] = {}
    for item in result.allocation:
        normalized_category = normalize_service_category(item.category)
        category = categories_by_normalized_name.get(normalized_category)
        if category is None:
            raise ValueError(
                f"unknown budget allocation category returned by AI: {item.category!r}"
            )
        if category in requested_allocations:
            raise ValueError(f"duplicate budget allocation category: {category}")
        amount = float(item.amount)
        if not math.isfinite(amount) or amount <= 0:
            raise ValueError(
                f"budget allocation for {category} must be a finite positive amount"
            )
        requested_allocations[category] = amount

    missing_categories = [
        category
        for category in state.service_categories
        if category not in requested_allocations
    ]
    if missing_categories:
        raise ValueError(
            "budget allocation is missing service categories: "
            + ", ".join(missing_categories)
        )

    allocation = {
        category: requested_allocations[category]
        for category in state.service_categories
    }
    if "Contingency" in requested_allocations:
        allocation["Contingency"] = requested_allocations["Contingency"]
    allocation = _rebalance(allocation, budget)
    return {"budget_allocation": allocation}
