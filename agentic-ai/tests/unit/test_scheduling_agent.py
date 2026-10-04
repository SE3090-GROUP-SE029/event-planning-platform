import asyncio
from typing import Any

from src.agents.scheduling_agent import SchedulingAgent
from src.models.scheduling_models import ActivityItem, ScheduleState


class _FakeGeminiClient:
    def __init__(self) -> None:
        self.prompt: str | None = None
        self.schema: type | None = None
        self.node_name: str | None = None

    async def generate_with_prompt(
        self, prompt: str, schema: type, **kwargs: Any
    ) -> Any:
        self.prompt = prompt
        self.schema = schema
        self.node_name = kwargs.get("node_name")
        return schema(
            activities=[
                ActivityItem(
                    title="Venue setup",
                    description="Prepare the event space and check equipment.",
                    start_time="2027-01-01T08:00:00Z",
                    end_time="2027-01-01T09:00:00Z",
                    vendor_type="Hospitality",
                ),
                ActivityItem(
                    title="AV check",
                    description="Test microphones, projection, and playback.",
                    start_time="2027-01-01T09:00:00Z",
                    end_time="2027-01-01T09:30:00Z",
                    vendor_type="AudioVisual",
                ),
            ]
        )


def _state() -> ScheduleState:
    return ScheduleState(
        event_id="event-1",
        title="Annual Gala",
        event_description="Awards dinner with live music",
        event_type="CORPORATE",
        date="2027-01-01",
        start_time="18:00:00",
        end_time="23:00:00",
        guest_count=120,
        requirements="Keynote and lunch service",
        vendor_service_context=[{"serviceType": "Catering"}],
    )


def test_scheduling_agent_uses_gemini_prompt_generation() -> None:
    client = _FakeGeminiClient()
    response = asyncio.run(SchedulingAgent(client).run(_state()))

    assert client.node_name == "generate_schedule"
    assert client.schema is not None
    assert client.schema.__name__ == "GeneratedList"
    assert client.prompt is not None
    assert "Annual Gala" in client.prompt
    assert "Awards dinner with live music" in client.prompt
    assert "Catering" in client.prompt
    assert "realistic durations" in client.prompt
    assert "expert AI Event Scheduling Agent" in client.prompt
    assert len(response.activities) == 2
    assert response.conflicts == []
