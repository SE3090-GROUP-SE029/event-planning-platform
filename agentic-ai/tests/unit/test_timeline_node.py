import asyncio
from typing import Any
from uuid import uuid4

import pytest
from google.generativeai import protos

from src.coordinator_agent.nodes import timeline
from src.coordinator_agent.nodes.timeline import TimelineOutput
from src.coordinator_agent.state import CoordinatorState
from src.gemini_client.schema import pydantic_to_gemini_schema


class _FakeGeminiClient:
    def __init__(self, result: TimelineOutput) -> None:
        self.result = result

    async def generate_with_prompt(
        self, _prompt: str, _schema: type, **_kwargs: Any
    ) -> TimelineOutput:
        return self.result


def _state() -> CoordinatorState:
    return CoordinatorState(
        event_id=uuid4(),
        event={"date": "2027-01-01", "guest_count": 100},
        service_categories=["Catering"],
        target_vendor_types=["Caterer"],
    )


def test_timeline_schema_is_accepted_by_gemini_sdk() -> None:
    schema = pydantic_to_gemini_schema(TimelineOutput)

    assert schema["properties"]["phases"]["type_"] == "ARRAY"
    protos.Schema(schema)


def test_propose_timeline_converts_structured_phases_to_state_mapping(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    result = TimelineOutput(
        phases=[
            {"phase_name": "  Planning  ", "timing": "  12 weeks before  "},
            {"phase_name": "Booking", "timing": "8 weeks before"},
            {"phase_name": "Confirmation", "timing": "1 week before"},
        ]
    )
    monkeypatch.setattr(timeline, "GeminiClient", lambda: _FakeGeminiClient(result))

    updates = asyncio.run(timeline.propose_timeline(_state()))

    assert updates == {
        "proposed_timeline": {
            "Planning": "12 weeks before",
            "Booking": "8 weeks before",
            "Confirmation": "1 week before",
        }
    }


def test_propose_timeline_rejects_duplicate_normalized_phase_names(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    result = TimelineOutput(
        phases=[
            {"phase_name": "Planning", "timing": "12 weeks before"},
            {"phase_name": "Planning", "timing": "10 weeks before"},
            {"phase_name": "Confirmation", "timing": "1 week before"},
        ]
    )
    monkeypatch.setattr(timeline, "GeminiClient", lambda: _FakeGeminiClient(result))

    with pytest.raises(ValueError, match="duplicate phase name"):
        asyncio.run(timeline.propose_timeline(_state()))
