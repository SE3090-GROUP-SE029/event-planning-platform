import asyncio
from typing import Any
from uuid import uuid4

import pytest
from pydantic import BaseModel

from src.coordinator_agent.nodes import assess
from src.coordinator_agent.nodes.assess import BudgetOutput
from src.coordinator_agent.state import CoordinatorState


class _FakeGeminiClient:
    def __init__(self, result: BudgetOutput) -> None:
        self.result = result

    async def generate_with_prompt(
        self, _prompt: str, _schema: type[BaseModel], **_kwargs: Any
    ) -> BudgetOutput:
        return self.result


def _state() -> CoordinatorState:
    return CoordinatorState(
        event_id=uuid4(),
        event={"budget": 1000, "guest_count": 100},
        service_categories=["Catering", "Audio Visual"],
        target_vendor_types=["Catering provider", "Audio Visual provider"],
    )


def test_allocate_budget_normalizes_category_names_and_uses_full_budget(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    result = BudgetOutput(
        allocation=[
            {"category": " catering ", "amount": 400},
            {"category": "audio-visual", "amount": 300},
            {"category": "CONTINGENCY", "amount": 100},
        ]
    )
    monkeypatch.setattr(assess, "GeminiClient", lambda: _FakeGeminiClient(result))

    updates = asyncio.run(assess.allocate_budget(_state()))

    allocation = updates["budget_allocation"]
    assert set(allocation) == {"Catering", "Audio Visual", "Contingency"}
    assert sum(allocation.values()) == 1000
    assert allocation == {
        "Catering": 500.0,
        "Audio Visual": 375.0,
        "Contingency": 125.0,
    }


def test_allocate_budget_reports_unmatched_or_missing_categories(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    result = BudgetOutput(allocation=[{"category": "Catering", "amount": 100}])
    monkeypatch.setattr(assess, "GeminiClient", lambda: _FakeGeminiClient(result))

    with pytest.raises(ValueError, match="missing service categories: Audio Visual"):
        asyncio.run(assess.allocate_budget(_state()))


def test_allocate_budget_rejects_non_positive_event_budget(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    state = _state()
    state.event["budget"] = 0
    monkeypatch.setattr(
        assess,
        "GeminiClient",
        lambda: pytest.fail("Gemini must not be called for an invalid budget"),
    )

    with pytest.raises(ValueError, match="finite positive amount"):
        asyncio.run(assess.allocate_budget(state))
