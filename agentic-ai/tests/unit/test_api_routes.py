import asyncio
from uuid import uuid4

import httpx
import pytest
from fastapi import FastAPI

from src.api import routes
from src.coordinator_agent.execution import (
    CoordinatorProviderError,
    CoordinatorValidationError,
)


def _request_status(
    monkeypatch: pytest.MonkeyPatch, failure: Exception
) -> httpx.Response:
    async def fail_execution(*_args: object, **_kwargs: object) -> object:
        raise failure

    monkeypatch.setattr(routes, "execute_coordinator_agent", fail_execution)
    app = FastAPI()
    app.include_router(routes.router)

    async def send_request() -> httpx.Response:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            return await client.post(
                "/api/coordinator/generate",
                json={"eventId": str(uuid4()), "event": {}},
            )

    return asyncio.run(send_request())


def test_coordinator_route_returns_gateway_timeout_for_execution_timeout(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    response = _request_status(monkeypatch, TimeoutError("internal timeout"))

    assert response.status_code == 504
    assert response.json()["detail"] == "Plan generation took too long. Please try again."


def test_coordinator_route_returns_bad_gateway_for_provider_failure(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    response = _request_status(
        monkeypatch, CoordinatorProviderError("internal provider details")
    )

    assert response.status_code == 502
    assert response.json()["detail"] == (
        "The AI provider could not generate a plan. Please try again later."
    )


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
