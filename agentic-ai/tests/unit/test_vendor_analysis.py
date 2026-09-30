"""Unit tests for the C2 vendor analysis agent."""

from __future__ import annotations

import asyncio
from uuid import uuid4

import httpx
import pytest
from fastapi import FastAPI
from pydantic import ValidationError

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
    VendorAnalysisProviderError,
    VendorAnalysisValidationError,
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


def test_request_requires_at_least_one_candidate() -> None:
    with pytest.raises(ValidationError):
        VendorRecommendationRequest(
            eventId=uuid4(),
            planId=uuid4(),
            plan=VendorAnalysisPlanInput(serviceCategories=["Catering"]),
            candidates=[],
        )
