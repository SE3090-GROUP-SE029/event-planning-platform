import asyncio
import json
from typing import Any

import pytest

from src.coordinator_agent.models import CoordinatorPlanOutput
from src.gemini_client.client import GeminiClient
from src.gemini_client.exceptions import GeminiResponseError, GeminiTokenLimitError


def _plan_payload() -> dict[str, Any]:
    return {
        "service_categories": ["Catering"],
        "budget_allocation": [],
        "target_vendor_types": ["Caterer"],
        "proposed_timeline": [
            {"phase_name": "A", "timing": "Now", "description": "Do A"},
            {"phase_name": "B", "timing": "Later", "description": "Do B"},
            {"phase_name": "C", "timing": "Event", "description": "Do C"},
        ],
        "rationale": "A suitable plan",
        "identified_risks": [],
        "missing_requirements": [],
        "plan_completeness_score": 90,
        "validation_summary": "Valid",
    }


class _Response:
    text = json.dumps(_plan_payload())


class _Model:
    def count_tokens(self, prompt: str) -> Any:
        return type("TokenCount", (), {"total_tokens": len(prompt) // 4})()

    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        return _Response()


def test_client_generates_validated_plan_without_api_key() -> None:
    client = GeminiClient(model=_Model())
    plan = asyncio.run(client.generate_plan({"name": "Gala"}, CoordinatorPlanOutput))
    assert isinstance(plan, CoordinatorPlanOutput)


def test_client_rejects_token_limit() -> None:
    client = GeminiClient(model=_Model(), max_input_tokens=1)
    with pytest.raises(GeminiTokenLimitError):
        asyncio.run(client.generate_plan({"name": "Gala"}, CoordinatorPlanOutput))


def test_client_rejects_invalid_response() -> None:
    client = GeminiClient(model=_Model())
    with pytest.raises(GeminiResponseError):
        client.validate_response({"plan_completeness_score": 101}, CoordinatorPlanOutput)
