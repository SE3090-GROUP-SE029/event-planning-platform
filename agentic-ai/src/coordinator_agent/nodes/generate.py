"""Service-category identification node."""

import json
import logging
import re

from pydantic import BaseModel, Field

from src.coordinator_agent.prompts import SERVICE_CATEGORY_PROMPT
from src.gemini_client.client import GeminiClient
from ..state import CoordinatorState

logger = logging.getLogger(__name__)


class ServiceCategoriesOutput(BaseModel):
    categories: list[str] = Field(..., min_length=1, max_length=12)


def _is_valid_category(category: str) -> bool:
    return bool(re.fullmatch(r"[A-Za-z][A-Za-z -]{1,49}", category.strip()))


def _default_categories(event: dict[str, object]) -> list[str]:
    defaults = ["Catering", "Venue", "Photography"]
    event_type = str(event.get("type", "")).lower()
    if "corporate" in event_type or "gala" in event_type:
        defaults.extend(["Audio Visual", "Entertainment"])
    return defaults


async def identify_service_categories(state: CoordinatorState) -> dict[str, list[str]]:
    """Identify generic service categories and reject vendor-like values."""

    prompt = SERVICE_CATEGORY_PROMPT.format(
        event_details=json.dumps(state.event, ensure_ascii=True, default=str),
        analysis=state.requirements_analysis,
    )
    result = await GeminiClient().generate_with_prompt(prompt, ServiceCategoriesOutput)
    categories = {
        category.strip()
        for category in result.categories
        if _is_valid_category(category)
    }
    if len(categories) < 3:
        logger.warning("Fewer than three valid categories returned; adding defaults")
        categories.update(_default_categories(state.event))
    ordered = sorted(categories)[:12]
    return {
        "service_categories": ordered,
        "target_vendor_types": [f"{category} provider" for category in ordered],
    }


async def generate_plan(state: CoordinatorState) -> dict[str, list[str]]:
    """Backward-compatible alias for the category node."""

    return await identify_service_categories(state)
