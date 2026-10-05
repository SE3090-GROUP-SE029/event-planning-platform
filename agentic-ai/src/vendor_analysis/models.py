"""Structured request/response models for vendor recommendation."""

from typing import Any
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator


class VendorAnalysisPlanInput(BaseModel):
    """Plan + event snapshot fields used for ranking."""

    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    event_type: str | None = Field(default=None, alias="eventType", max_length=100)
    event_date: str | None = Field(default=None, alias="eventDate", max_length=64)
    guest_count: int | None = Field(default=None, alias="guestCount", ge=1)
    budget: float | None = Field(default=None, gt=0, allow_inf_nan=False)
    requirements: str | None = Field(default=None, max_length=4000)
    service_categories: list[str] = Field(
        default_factory=list, alias="serviceCategories", max_length=30
    )
    target_vendor_types: list[str] = Field(
        default_factory=list, alias="targetVendorTypes", max_length=30
    )
    budget_allocation: dict[str, float] = Field(
        default_factory=dict, alias="budgetAllocation"
    )


class VendorAnalysisServiceInput(BaseModel):
    """One pre-filtered service offered by a candidate vendor."""

    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    vendor_service_id: UUID = Field(..., alias="vendorServiceId")
    service_name: str = Field(..., alias="serviceName", min_length=1, max_length=200)
    price: float | None = Field(default=None, ge=0, allow_inf_nan=False)
    pricing_type: str | None = Field(default=None, alias="pricingType", max_length=40)
    effective_price: float | None = Field(
        default=None, alias="effectivePrice", ge=0, allow_inf_nan=False
    )


class VendorAnalysisCandidateInput(BaseModel):
    """One pre-filtered APPROVED vendor candidate from .NET."""

    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    vendor_id: UUID = Field(..., alias="vendorId")
    business_name: str = Field(..., alias="businessName", min_length=1, max_length=200)
    category: str = Field(..., min_length=1, max_length=50)
    description: str | None = Field(default=None, max_length=1000)
    average_rating: float | None = Field(
        default=None, alias="averageRating", ge=0, le=5, allow_inf_nan=False
    )
    review_count: int = Field(default=0, alias="reviewCount", ge=0)
    availability_match: bool = Field(default=False, alias="availabilityMatch")
    matched_service: VendorAnalysisServiceInput | None = Field(
        default=None, alias="matchedService"
    )
    budget_fit: bool = Field(default=True, alias="budgetFit")


class VendorRecommendationRequest(BaseModel):
    """Backend request: plan summary + candidate vendors only."""

    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    event_id: UUID = Field(..., alias="eventId")
    plan_id: UUID = Field(..., alias="planId")
    plan: VendorAnalysisPlanInput
    candidates: list[VendorAnalysisCandidateInput] = Field(..., min_length=1, max_length=50)

    @field_validator("event_id", "plan_id", mode="before")
    @classmethod
    def parse_uuid(cls, value: object) -> UUID:
        if isinstance(value, UUID):
            return value
        if not isinstance(value, str):
            raise ValueError("must be a UUID string")
        try:
            return UUID(value)
        except ValueError as exc:
            raise ValueError("must be a valid UUID") from exc


class VendorRecommendationItemOutput(BaseModel):
    """One ranked recommendation. Must reference a supplied candidate only."""

    model_config = ConfigDict(extra="forbid", populate_by_name=True)

    vendor_id: UUID = Field(..., alias="vendorId")
    vendor_service_id: UUID | None = Field(default=None, alias="vendorServiceId")
    score: int = Field(..., ge=0, le=100)
    reasons: list[str] = Field(..., min_length=1, max_length=5)

    @field_validator("reasons")
    @classmethod
    def reject_blank_reasons(cls, value: list[str]) -> list[str]:
        cleaned = [item.strip() for item in value if isinstance(item, str) and item.strip()]
        if not cleaned:
            raise ValueError("at least one non-blank reason is required")
        return cleaned[:5]


class VendorRecommendationResponse(BaseModel):
    """Structured ranking of the supplied candidates."""

    model_config = ConfigDict(
        extra="forbid",
        populate_by_name=True,
        json_schema_extra={
            "examples": [
                {
                    "recommendations": [
                        {
                            "vendorId": "00000000-0000-0000-0000-000000000001",
                            "vendorServiceId": "00000000-0000-0000-0000-000000000002",
                            "score": 88,
                            "reasons": ["Matches catering budget and guest count."],
                        }
                    ]
                }
            ]
        },
    )

    recommendations: list[VendorRecommendationItemOutput] = Field(
        ..., min_length=1, max_length=50
    )

    @model_validator(mode="after")
    def ensure_unique_vendors(self) -> "VendorRecommendationResponse":
        ids = [item.vendor_id for item in self.recommendations]
        if len(ids) != len(set(ids)):
            raise ValueError("recommendations must not contain duplicate vendorId values")
        return self

    @classmethod
    def get_json_schema_for_gemini(cls) -> dict[str, Any]:
        from src.gemini_client.schema import pydantic_to_gemini_json_schema

        return pydantic_to_gemini_json_schema(cls)
