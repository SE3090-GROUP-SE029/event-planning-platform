import asyncio
from collections.abc import Callable
from uuid import uuid4

import httpx
import pytest
from fastapi import FastAPI

from src.api import routes
from src.coordinator_agent.execution import (
    CoordinatorProviderError,
    CoordinatorValidationError,
)
from src.gemini_client.exceptions import (
    GeminiClientError,
    GeminiQuotaError,
    GeminiTimeoutError,
)


def _valid_generate_payload() -> dict[str, object]:
    return {
        "eventId": str(uuid4()),
        "event": {
            "name": "Gala",
            "type": "WEDDING",
            "date": "2027-06-15T18:00:00",
            "location": "Town Hall",
            "guest_count": 100,
            "budget": 12500.50,
            "requirements": "Vegetarian catering",
        },
    }


async def _post_generate(payload: dict[str, object]) -> httpx.Response:
    app = FastAPI()
    app.include_router(routes.router)
    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        return await client.post("/api/coordinator/generate", json=payload)


def _valid_schedule_payload() -> dict[str, object]:
    return {
        "eventId": str(uuid4()),
        "eventTitle": "Gala",
        "eventDescription": "Vegetarian catering",
        "eventType": "CORPORATE",
        "eventDate": "2027-06-15",
        "eventStartTime": "18:00:00",
        "eventEndTime": "22:00:00",
        "vendorServiceContext": [{"serviceType": "Catering"}],
        "guestCount": 100,
    }


async def _post_schedule_generate(gemini_client: object) -> httpx.Response:
    app = FastAPI()
    app.state.gemini_client = gemini_client
    app.include_router(routes.schedule_router)
    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        return await client.post("/api/schedules/generate", json=_valid_schedule_payload())


def _request_status(
    monkeypatch: pytest.MonkeyPatch, failure: Exception
) -> httpx.Response:
    async def fail_execution(*_args: object, **_kwargs: object) -> object:
        raise failure

    monkeypatch.setattr(routes, "execute_coordinator_agent", fail_execution)
    return asyncio.run(_post_generate(_valid_generate_payload()))


def test_coordinator_route_accepts_backend_event_contract(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    event_id = str(uuid4())
    payload = _valid_generate_payload()
    payload["eventId"] = event_id
    calls: list[tuple[str, dict[str, object]]] = []
    timeouts: list[object] = []

    async def execute(event_id_arg: str, event: dict[str, object], **_kwargs: object) -> object:
        calls.append((event_id_arg, event))
        timeouts.append(_kwargs.get("timeout_seconds"))
        return {
            "service_categories": ["Catering"],
            "target_vendor_types": ["Caterer"],
            "proposed_timeline": [
                {"phase_name": "Planning", "timing": "12 weeks before", "description": "Plan"},
                {"phase_name": "Booking", "timing": "8 weeks before", "description": "Book"},
                {"phase_name": "Confirmation", "timing": "1 week before", "description": "Confirm"},
            ],
            "budget_allocation": [
                {
                    "category": "Catering",
                    "amount": 100.0,
                    "percentage_of_total": 100.0,
                }
            ],
            "identified_risks": [
                {
                    "risk": "A supplier may become unavailable.",
                    "severity": "Medium",
                    "recommendation": "Confirm bookings and retain a backup option.",
                }
            ],
            "rationale": (
                "This complete plan balances the event priorities, confirms vendors "
                "early, and reserves adequate time for final confirmations."
            ),
            "plan_completeness_score": 90,
            "validation_summary": "Valid",
        }

    monkeypatch.setattr(routes, "execute_coordinator_agent", execute)
    response = asyncio.run(_post_generate(payload))

    assert response.status_code == 200
    assert response.json()["service_categories"] == ["Catering"]
    assert response.json()["budget_allocation"][0]["amount"] == 100.0
    assert response.json()["identified_risks"][0]["severity"] == "Medium"
    assert len(response.json()["proposed_timeline"]) == 3
    assert len(calls) == 1
    assert calls[0][0] == event_id
    assert timeouts == [routes.AGENTIC_AI_REQUEST_TIMEOUT_SECONDS]
    assert set(calls[0][1]) == {
        "name",
        "type",
        "date",
        "location",
        "guest_count",
        "budget",
        "requirements",
    }


@pytest.mark.parametrize(
    "mutate",
    [
        lambda event: event.pop("requirements"),
        lambda event: event.update(type="wedding"),
        lambda event: event.update(date="next week"),
        lambda event: event.update(date=123),
        lambda event: event.update(date="2027-06-15"),
        lambda event: event.update(location={"name": "Town Hall"}),
        lambda event: event.update(guest_count="100"),
        lambda event: event.update(guest_count=True),
        lambda event: event.update(budget=-1),
        lambda event: event.update(budget=0),
        lambda event: event.update(unexpected="value"),
        lambda event: event.update(name="   "),
    ],
)
def test_malformed_event_is_rejected_before_workflow(
    monkeypatch: pytest.MonkeyPatch,
    mutate: Callable[[dict[str, object]], object],
) -> None:
    called = False

    async def execute(*_args: object, **_kwargs: object) -> object:
        nonlocal called
        called = True
        raise AssertionError("workflow must not run for an invalid request")

    monkeypatch.setattr(routes, "execute_coordinator_agent", execute)
    payload = _valid_generate_payload()
    event = payload["event"]
    assert isinstance(event, dict)
    mutate(event)

    response = asyncio.run(_post_generate(payload))

    assert response.status_code == 422
    assert not called


@pytest.mark.parametrize("event_id", [3, "not-a-uuid"])
def test_malformed_event_id_is_rejected_before_workflow(
    monkeypatch: pytest.MonkeyPatch, event_id: object
) -> None:
    called = False

    async def execute(*_args: object, **_kwargs: object) -> object:
        nonlocal called
        called = True
        raise AssertionError("workflow must not run for an invalid request")

    monkeypatch.setattr(routes, "execute_coordinator_agent", execute)
    payload = _valid_generate_payload()
    payload["eventId"] = event_id

    response = asyncio.run(_post_generate(payload))

    assert response.status_code == 422
    assert not called


def test_coordinator_route_returns_gateway_timeout_for_execution_timeout(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    response = _request_status(monkeypatch, TimeoutError("internal timeout"))

    assert response.status_code == 504
    assert response.json()["detail"] == {
        "code": "provider_timeout",
        "message": "Plan generation took too long. Please try again.",
    }


def test_coordinator_route_returns_bad_gateway_for_provider_failure(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    response = _request_status(
        monkeypatch, CoordinatorProviderError("internal provider details")
    )

    assert response.status_code == 502
    assert response.json()["detail"] == {
        "code": "provider_error",
        "message": "The AI provider could not generate a plan. Please try again later.",
    }


def test_coordinator_route_exposes_quota_category_and_retry_after(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    failure = CoordinatorProviderError(
        "internal provider details",
        code="quota_exhausted",
        status_code=429,
        retry_after=60,
    )

    response = _request_status(monkeypatch, failure)

    assert response.status_code == 429
    assert response.headers["retry-after"] == "60"
    assert response.json()["detail"] == {
        "code": "quota_exhausted",
        "message": "Gemini quota is exhausted. Retry later.",
    }
    assert "internal provider details" not in response.text


def test_coordinator_route_reports_all_gemini_keys_exhausted(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    failure = CoordinatorProviderError(
        "internal provider details",
        code="all_gemini_keys_exhausted",
        status_code=503,
        retry_after=600,
    )

    response = _request_status(monkeypatch, failure)

    assert response.status_code == 503
    assert response.headers["retry-after"] == "600"
    assert response.json()["detail"]["error"] == "all_gemini_keys_exhausted"
    assert response.json()["detail"]["available_keys"] == 0


def test_coordinator_route_returns_unprocessable_entity_for_invalid_plan(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    response = _request_status(
        monkeypatch, CoordinatorValidationError("internal validation details")
    )

    assert response.status_code == 422
    assert response.json()["detail"] == (
        "The generated plan did not pass validation. Please try again."
    )


@pytest.mark.parametrize(
    "failure",
    [
        GeminiQuotaError("internal quota details"),
        GeminiClientError("internal mixed fallback details"),
    ],
)
def test_schedule_route_returns_safe_unavailable_for_provider_failure(
    failure: GeminiClientError,
) -> None:
    class FailingGeminiClient:
        async def generate_with_prompt(self, *_args: object, **_kwargs: object) -> object:
            raise failure

    response = asyncio.run(_post_schedule_generate(FailingGeminiClient()))

    assert response.status_code == 503
    assert response.json() == {
        "detail": "AI schedule generation is temporarily unavailable. Please try again later."
    }
    assert str(failure) not in response.text
    assert "Traceback" not in response.text


def test_schedule_route_returns_gateway_timeout_for_provider_timeout() -> None:
    class TimedOutGeminiClient:
        async def generate_with_prompt(self, *_args: object, **_kwargs: object) -> object:
            raise GeminiTimeoutError("provider timed out")

    response = asyncio.run(_post_schedule_generate(TimedOutGeminiClient()))

    assert response.status_code == 504
    assert response.json()["detail"] == {
        "code": "provider_timeout",
        "message": routes.SCHEDULE_GENERATION_TIMEOUT_MESSAGE,
    }
    assert "provider timed out" not in response.text


def test_schedule_route_cancels_generation_at_configured_timeout(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setattr(routes, "SCHEDULE_GENERATION_TIMEOUT_SECONDS", 0.01)

    class SlowGeminiClient:
        async def generate_with_prompt(self, *_args: object, **_kwargs: object) -> object:
            await asyncio.sleep(1)
            return {"activities": []}

    response = asyncio.run(_post_schedule_generate(SlowGeminiClient()))

    assert response.status_code == 504
    assert response.json()["detail"]["message"] == (
        routes.SCHEDULE_GENERATION_TIMEOUT_MESSAGE
    )


def test_schedule_route_configures_gemini_timeout_to_30_minutes(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    configured_timeouts: list[float] = []

    class StubAgent:
        def __init__(self, _client: object) -> None:
            pass

        async def run(self, _state: object) -> dict[str, object]:
            return {"activities": [], "conflicts": []}

    def make_gemini_client(*, timeout: float) -> object:
        configured_timeouts.append(timeout)
        return object()

    monkeypatch.setattr(routes, "GeminiClient", make_gemini_client)
    monkeypatch.setattr(routes, "SchedulingAgent", StubAgent)

    response = asyncio.run(_post_schedule_generate(None))

    assert response.status_code == 200
    assert configured_timeouts == [1800.0]
