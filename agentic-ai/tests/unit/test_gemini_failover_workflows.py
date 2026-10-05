import asyncio
import json
from types import SimpleNamespace
from typing import Any
from uuid import uuid4

import pytest
from google.api_core.exceptions import ServiceUnavailable

from src.agents.scheduling_agent import SchedulingAgent
from src.coordinator_agent.config import Settings
from src.coordinator_agent.execution import execute_coordinator_agent
from src.coordinator_agent.nodes.assess import allocate_budget
from src.coordinator_agent.nodes.detect import assess_risks
from src.coordinator_agent.nodes.timeline import propose_timeline
from src.coordinator_agent.state import CoordinatorState
from src.gemini_client import client as gemini_client_module
from src.gemini_client.client import GeminiClient
from src.models.scheduling_models import ScheduleState
from src.vendor_analysis.models import (
    VendorAnalysisCandidateInput,
    VendorAnalysisPlanInput,
    VendorRecommendationRequest,
)
from src.vendor_analysis.service import execute_vendor_analysis


def _install_failing_primary(
    monkeypatch: pytest.MonkeyPatch,
    response: dict[str, Any],
) -> list[str]:
    settings = Settings(
        _env_file=None,
        gemini_api_key="workflow-primary-test-key",
        gemini_api_keys="workflow-secondary-test-key",
        gemini_model="gemini-3.8-flash",
        gemini_max_retries=5,
    )
    calls: list[str] = []

    class _WorkflowModel:
        def __init__(self, api_key: str) -> None:
            self.api_key = api_key

        async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> Any:
            calls.append(self.api_key)
            if self.api_key == settings.get_gemini_api_keys()[0]:
                raise ServiceUnavailable("HTTP 503 Service Unavailable")
            return SimpleNamespace(text=json.dumps(response))

    async def no_delay(_seconds: float) -> None:
        raise AssertionError("server-error retries must not add backoff")

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(
        gemini_client_module,
        "_configured_model",
        lambda _model_name, api_key: _WorkflowModel(api_key),
    )
    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", no_delay)
    return calls


def _assert_rotated(calls: list[str]) -> None:
    assert calls == [
        "workflow-primary-test-key",
        "workflow-primary-test-key",
        "workflow-secondary-test-key",
    ]


def test_coordinator_generation_continues_after_503(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="coordinator-primary-test-key",
        gemini_api_keys="coordinator-secondary-test-key",
        gemini_model="gemini-3.8-flash",
        gemini_max_retries=5,
    )
    responses = iter(
        [
            {
                "analysis": (
                    "The event requires catering, a venue, and photography."
                )
            },
            {"categories": ["Catering", "Venue", "Photography"]},
            {
                "phases": [
                    {"phase_name": "Planning", "timing": "12 weeks before"},
                    {"phase_name": "Booking", "timing": "8 weeks before"},
                    {"phase_name": "Confirmation", "timing": "1 week before"},
                ]
            },
            {
                "allocation": [
                    {"category": "Catering", "amount": 1},
                    {"category": "Venue", "amount": 1},
                    {"category": "Photography", "amount": 1},
                ]
            },
            {
                "risks": [
                    {
                        "risk": "A supplier may become unavailable.",
                        "severity": "Medium",
                        "recommendation": "Confirm the booking and keep a backup.",
                    }
                ]
            },
            {"requirements": []},
            {
                "rationale": (
                    "Catering, venue, and photography cover the event's core needs. "
                    "The budget is distributed evenly across these services, while "
                    "the timeline provides clear milestones for planning, booking, "
                    "and final confirmation before the event."
                )
            },
        ]
    )
    calls: list[str] = []

    class _CoordinatorModel:
        def __init__(self, api_key: str) -> None:
            self.api_key = api_key

        async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> Any:
            calls.append(self.api_key)
            if self.api_key == settings.get_gemini_api_keys()[0]:
                raise ServiceUnavailable("HTTP 503 Service Unavailable")
            return SimpleNamespace(text=json.dumps(next(responses)))

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(
        gemini_client_module,
        "_configured_model",
        lambda _model_name, api_key: _CoordinatorModel(api_key),
    )

    async def no_delay(_seconds: float) -> None:
        raise AssertionError("coordinator retries must not add backoff")

    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", no_delay)

    result = asyncio.run(
        execute_coordinator_agent(
            str(uuid4()),
            {
                "type": "WEDDING",
                "date": "2027-06-15",
                "guest_count": 100,
                "budget": 1000,
            },
            timeout_seconds=30,
        )
    )

    assert result.service_categories == ["Catering", "Photography", "Venue"]
    assert len(result.proposed_timeline) == 3
    assert result.identified_risks
    assert len(calls) == 9
    assert calls[:2] == [
        "coordinator-primary-test-key",
        "coordinator-primary-test-key",
    ]
    assert set(calls[2:]) == {"coordinator-secondary-test-key"}


def test_schedule_generation_continues_after_503(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    calls = _install_failing_primary(
        monkeypatch,
        {
            "activities": [
                {
                    "title": "Venue setup",
                    "description": "Prepare and inspect the event space.",
                    "start_time": "2027-01-01T08:00:00",
                    "end_time": "2027-01-01T09:00:00",
                    "vendor_type": "Hospitality",
                }
            ]
        },
    )
    state = ScheduleState(
        event_id="event-1",
        title="Annual Gala",
        event_description="Awards dinner",
        event_type="CORPORATE",
        date="2027-01-01",
        start_time="18:00:00",
        end_time="23:00:00",
        guest_count=120,
        requirements="Keynote and dinner",
    )

    result = asyncio.run(SchedulingAgent(GeminiClient()).run(state))

    assert [activity.title for activity in result.activities] == ["Venue setup"]
    _assert_rotated(calls)


def test_vendor_recommendation_continues_after_503(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    vendor_id = uuid4()
    calls = _install_failing_primary(
        monkeypatch,
        {
            "recommendations": [
                {
                    "vendorId": str(vendor_id),
                    "vendorServiceId": None,
                    "score": 90,
                    "reasons": ["Matches the requested catering service."],
                }
            ]
        },
    )
    request = VendorRecommendationRequest(
        eventId=uuid4(),
        planId=uuid4(),
        plan=VendorAnalysisPlanInput(serviceCategories=["Catering"]),
        candidates=[
            VendorAnalysisCandidateInput(
                vendorId=vendor_id,
                businessName="Sample Caterer",
                category="Catering",
            )
        ],
    )

    result = asyncio.run(execute_vendor_analysis(request, GeminiClient()))

    assert result.recommendations[0].vendor_id == vendor_id
    _assert_rotated(calls)


def test_risk_assessment_continues_after_503(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    calls = _install_failing_primary(
        monkeypatch,
        {
            "risks": [
                {
                    "risk": "A supplier may become unavailable.",
                    "severity": "Medium",
                    "recommendation": "Confirm the booking and keep a backup.",
                }
            ]
        },
    )
    state = CoordinatorState(
        event_id=uuid4(),
        event={"type": "Gala"},
        requirements_analysis="A supplier should be confirmed early.",
        budget_allocation={"Catering": 1000},
    )

    result = asyncio.run(assess_risks(state))

    assert result["identified_risks"][0].severity == "Medium"
    _assert_rotated(calls)


def test_timeline_generation_continues_after_503(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    calls = _install_failing_primary(
        monkeypatch,
        {
            "phases": [
                {"phase_name": "Planning", "timing": "12 weeks before"},
                {"phase_name": "Booking", "timing": "8 weeks before"},
                {"phase_name": "Confirmation", "timing": "1 week before"},
            ]
        },
    )
    state = CoordinatorState(
        event_id=uuid4(),
        event={"date": "2027-01-01", "guest_count": 100},
        service_categories=["Catering"],
        target_vendor_types=["Catering provider"],
    )

    result = asyncio.run(propose_timeline(state))

    assert len(result["proposed_timeline"]) == 3
    _assert_rotated(calls)


def test_budget_allocation_continues_after_503(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    calls = _install_failing_primary(
        monkeypatch,
        {"allocation": [{"category": "Catering", "amount": 1000}]},
    )
    state = CoordinatorState(
        event_id=uuid4(),
        event={"budget": 1000, "guest_count": 100},
        service_categories=["Catering"],
        target_vendor_types=["Catering provider"],
    )

    result = asyncio.run(allocate_budget(state))

    assert result["budget_allocation"] == {"Catering": 1000.0}
    _assert_rotated(calls)
