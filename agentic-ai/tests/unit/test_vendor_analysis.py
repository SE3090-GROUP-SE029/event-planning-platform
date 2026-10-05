"""Unit tests for the C2 vendor analysis agent."""

from __future__ import annotations

import asyncio
from uuid import uuid4

import httpx
import pytest
from fastapi import FastAPI
from pydantic import ValidationError

from src.gemini_client import client as gemini_client_module
from src.gemini_client.exceptions import GeminiInvalidRequestError
from src.vendor_analysis import routes
from src.vendor_analysis.models import (
    VendorAnalysisCandidateInput,
    VendorAnalysisPlanInput,
    VendorAnalysisServiceInput,
    VendorRecommendationItemOutput,
    VendorRecommendationRequest,
    VendorRecommendationResponse,
)
from src.vendor_analysis.service import (
    VendorAnalysisConfigurationError,
    VendorAnalysisProviderError,
    VendorAnalysisValidationError,
    execute_vendor_analysis,
    filter_to_candidates,
)


def _candidate(
    vendor_id=None,
    service_id=None,
    category: str = "CATERING",
) -> VendorAnalysisCandidateInput:
    vendor_id = vendor_id or uuid4()
    service_id = service_id or uuid4()
    return VendorAnalysisCandidateInput(
        vendorId=vendor_id,
        businessName="Taste Catering",
        category=category,
        averageRating=4.5,
        reviewCount=12,
        availabilityMatch=True,
        budgetFit=True,
        matchedService=VendorAnalysisServiceInput(
            vendorServiceId=service_id,
            serviceName="Buffet",
            price=50,
            pricingType="PER_PERSON",
            effectivePrice=5000,
        ),
    )


def _request(candidates: list[VendorAnalysisCandidateInput] | None = None) -> VendorRecommendationRequest:
    items = candidates or [_candidate()]
    return VendorRecommendationRequest(
        eventId=uuid4(),
        planId=uuid4(),
        plan=VendorAnalysisPlanInput(
            eventType="WEDDING",
            guestCount=100,
            budget=10000,
            serviceCategories=["Catering"],
            targetVendorTypes=["Catering provider"],
            budgetAllocation={"Catering": 5000},
        ),
        candidates=items,
    )


def test_schema_route_returns_sanitized_interactions_json_schema() -> None:
    app = FastAPI()
    app.include_router(routes.router)

    async def call() -> httpx.Response:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            return await client.get("/api/vendor-analysis/schema")

    response = asyncio.run(call())
    assert response.status_code == 200
    body = response.json()
    assert body["properties"]["recommendations"]["type"] == "array"
    assert "minItems" not in body["properties"]["recommendations"]
    assert "maxItems" not in body["properties"]["recommendations"]
    item_properties = body["properties"]["recommendations"]["items"]["properties"]
    assert item_properties["vendorId"] == {"type": "string"}
    assert item_properties["vendorServiceId"] == {
        "type": "string",
        "nullable": True,
    }


def test_response_schema_requires_reasons_and_score_bounds() -> None:
    with pytest.raises(ValidationError):
        VendorRecommendationItemOutput(
            vendorId=uuid4(),
            score=101,
            reasons=["ok"],
        )
    with pytest.raises(ValidationError):
        VendorRecommendationItemOutput(
            vendorId=uuid4(),
            score=80,
            reasons=["   "],
        )
    with pytest.raises(ValidationError):
        VendorRecommendationItemOutput(
            vendorId=uuid4(),
            score=80,
            reasons=[],
        )
    with pytest.raises(ValidationError):
        VendorRecommendationItemOutput(
            vendorId=uuid4(),
            score=80,
            reasons=["reason"] * 6,
        )


def test_filter_drops_unknown_vendor_ids() -> None:
    known = _candidate()
    request = _request([known])
    response = VendorRecommendationResponse(
        recommendations=[
            VendorRecommendationItemOutput(
                vendorId=uuid4(),
                score=99,
                reasons=["Invented vendor"],
            ),
            VendorRecommendationItemOutput(
                vendorId=known.vendor_id,
                vendorServiceId=known.matched_service.vendor_service_id,
                score=90,
                reasons=["Good catering fit"],
            ),
        ]
    )

    filtered = filter_to_candidates(response, request)

    assert len(filtered.recommendations) == 1
    assert filtered.recommendations[0].vendor_id == known.vendor_id


def test_filter_ranks_multiple_known_candidates() -> None:
    first = _candidate()
    second = _candidate()
    request = _request([first, second])
    response = VendorRecommendationResponse(
        recommendations=[
            VendorRecommendationItemOutput(
                vendorId=first.vendor_id,
                score=72,
                reasons=["Good fit"],
            ),
            VendorRecommendationItemOutput(
                vendorId=second.vendor_id,
                vendorServiceId=None,
                score=91,
                reasons=["Excellent fit"],
            ),
        ]
    )

    filtered = filter_to_candidates(response, request)

    assert [item.vendor_id for item in filtered.recommendations] == [
        second.vendor_id,
        first.vendor_id,
    ]
    assert filtered.recommendations[0].vendor_service_id == second.matched_service.vendor_service_id


def test_filter_rejects_when_all_ids_unknown() -> None:
    request = _request()
    response = VendorRecommendationResponse(
        recommendations=[
            VendorRecommendationItemOutput(
                vendorId=uuid4(),
                score=70,
                reasons=["Unknown"],
            )
        ]
    )

    with pytest.raises(VendorAnalysisValidationError):
        filter_to_candidates(response, request)


def test_filter_replaces_invalid_service_id_with_matched_service() -> None:
    known = _candidate()
    request = _request([known])
    response = VendorRecommendationResponse(
        recommendations=[
            VendorRecommendationItemOutput(
                vendorId=known.vendor_id,
                vendorServiceId=uuid4(),
                score=80,
                reasons=["Fit"],
            )
        ]
    )

    filtered = filter_to_candidates(response, request)
    assert (
        filtered.recommendations[0].vendor_service_id
        == known.matched_service.vendor_service_id
    )


def test_recommend_route_maps_provider_timeout(monkeypatch: pytest.MonkeyPatch) -> None:
    async def fail(*_args, **_kwargs):
        raise VendorAnalysisProviderError("timeout", code="provider_timeout", status_code=504)

    monkeypatch.setattr(routes, "execute_vendor_analysis", fail)

    app = FastAPI()
    app.include_router(routes.router)
    payload = _request().model_dump(mode="json", by_alias=True)

    async def call() -> httpx.Response:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            return await client.post("/api/vendor-analysis/recommend", json=payload)

    response = asyncio.run(call())
    assert response.status_code == 504
    assert response.json()["detail"]["code"] == "provider_timeout"


def test_recommend_route_enforces_30_minute_request_timeout(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setattr(routes, "AGENTIC_AI_REQUEST_TIMEOUT_SECONDS", 0.01)

    async def slow(*_args, **_kwargs):
        await asyncio.sleep(1)
        return VendorRecommendationResponse(recommendations=[])

    monkeypatch.setattr(routes, "execute_vendor_analysis", slow)

    app = FastAPI()
    app.include_router(routes.router)
    payload = _request().model_dump(mode="json", by_alias=True)

    async def call() -> httpx.Response:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            return await client.post("/api/vendor-analysis/recommend", json=payload)

    response = asyncio.run(call())
    assert response.status_code == 504
    assert response.json()["detail"]["code"] == "provider_timeout"


def test_recommend_route_success(monkeypatch: pytest.MonkeyPatch) -> None:
    candidate = _candidate()
    request = _request([candidate])

    async def ok(req: VendorRecommendationRequest, **_kwargs):
        return VendorRecommendationResponse(
            recommendations=[
                VendorRecommendationItemOutput(
                    vendorId=candidate.vendor_id,
                    vendorServiceId=candidate.matched_service.vendor_service_id,
                    score=91,
                    reasons=["Matches budget and category"],
                )
            ]
        )

    monkeypatch.setattr(routes, "execute_vendor_analysis", ok)

    app = FastAPI()
    app.include_router(routes.router)
    payload = request.model_dump(mode="json", by_alias=True)

    async def call() -> httpx.Response:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            return await client.post("/api/vendor-analysis/recommend", json=payload)

    response = asyncio.run(call())
    assert response.status_code == 200
    body = response.json()
    assert body["recommendations"][0]["score"] == 91
    assert body["recommendations"][0]["vendorId"] == str(candidate.vendor_id)


def test_recommend_route_returns_200_through_real_service_with_fake_gemini(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    candidate = _candidate()

    class FakeGeminiClient:
        def __init__(self, **_kwargs) -> None:
            pass

        async def generate_with_prompt(self, prompt, schema, node_name):
            assert str(candidate.vendor_id) in prompt
            assert schema is VendorRecommendationResponse
            assert node_name == "vendor_analysis_recommend"
            return VendorRecommendationResponse(
                recommendations=[
                    VendorRecommendationItemOutput(
                        vendorId=candidate.vendor_id,
                        vendorServiceId=None,
                        score=91,
                        reasons=["Matches budget and category"],
                    )
                ]
            )

    monkeypatch.setattr(gemini_client_module, "GeminiClient", FakeGeminiClient)
    app = FastAPI()
    app.include_router(routes.router)
    payload = _request([candidate]).model_dump(mode="json", by_alias=True)

    async def call() -> httpx.Response:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            return await client.post("/api/vendor-analysis/recommend", json=payload)

    response = asyncio.run(call())
    assert response.status_code == 200
    assert response.json()["recommendations"][0]["vendorId"] == str(candidate.vendor_id)
    assert response.json()["recommendations"][0]["vendorServiceId"] == str(
        candidate.matched_service.vendor_service_id
    )


def test_recommend_route_rejects_empty_candidates_before_gemini(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    async def should_not_run(*_args, **_kwargs):
        raise AssertionError("Gemini must not be called for an empty candidate list")

    monkeypatch.setattr(routes, "execute_vendor_analysis", should_not_run)
    app = FastAPI()
    app.include_router(routes.router)
    payload = _request().model_dump(mode="json", by_alias=True)
    payload["candidates"] = []

    async def call() -> httpx.Response:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            return await client.post("/api/vendor-analysis/recommend", json=payload)

    response = asyncio.run(call())
    assert response.status_code == 422


def test_recommend_route_rejects_malformed_vendor_id_before_gemini(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    async def should_not_run(*_args, **_kwargs):
        raise AssertionError("Gemini must not be called for an invalid vendor ID")

    monkeypatch.setattr(routes, "execute_vendor_analysis", should_not_run)
    app = FastAPI()
    app.include_router(routes.router)
    payload = _request().model_dump(mode="json", by_alias=True)
    payload["candidates"][0]["vendorId"] = "not-a-uuid"

    async def call() -> httpx.Response:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            return await client.post("/api/vendor-analysis/recommend", json=payload)

    response = asyncio.run(call())
    assert response.status_code == 422


def test_recommend_route_reports_schema_rejection_separately(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    async def fail(*_args, **_kwargs):
        raise VendorAnalysisConfigurationError("invalid structured-output schema")

    monkeypatch.setattr(routes, "execute_vendor_analysis", fail)
    app = FastAPI()
    app.include_router(routes.router)
    payload = _request().model_dump(mode="json", by_alias=True)

    async def call() -> httpx.Response:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            return await client.post("/api/vendor-analysis/recommend", json=payload)

    response = asyncio.run(call())
    assert response.status_code == 500
    assert response.json()["detail"]["code"] == "gemini_request_invalid"


def test_service_maps_gemini_invalid_argument_to_configuration_error() -> None:
    class InvalidRequestGeminiClient:
        async def generate_with_prompt(self, *_args, **_kwargs):
            raise GeminiInvalidRequestError("structured-output schema rejected")

    async def test_call() -> None:
        with pytest.raises(VendorAnalysisConfigurationError):
            await execute_vendor_analysis(
                _request(),
                gemini_client=InvalidRequestGeminiClient(),
            )

    asyncio.run(test_call())


def test_request_requires_at_least_one_candidate() -> None:
    with pytest.raises(ValidationError):
        VendorRecommendationRequest(
            eventId=uuid4(),
            planId=uuid4(),
            plan=VendorAnalysisPlanInput(serviceCategories=["Catering"]),
            candidates=[],
        )
