"""Structured models used by Gemini and the coordinator response."""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator


class RiskModel(BaseModel):
    """A risk identified while planning an event."""

    risk: str = Field(..., max_length=500)
    severity: Literal["Low", "Medium", "High"]
    recommendation: str = Field(..., max_length=1000)


class MissingRequirementModel(BaseModel):
    """A requirement that must be clarified before execution."""

    requirement: str = Field(..., max_length=200)
    reason: str = Field(..., max_length=500)


class RiskOutput(RiskModel):
    """Gemini-compatible risk output."""


class MissingRequirementOutput(MissingRequirementModel):
    """Gemini-compatible missing-requirement output."""


class BudgetAllocationOutput(BaseModel):
    """A non-negative budget allocation."""

    category: str = Field(..., min_length=1)
    amount: float = Field(..., ge=0)
    percentage_of_total: float = Field(..., ge=0, le=100)


class TimelinePhaseOutput(BaseModel):
    """One phase in the proposed event timeline."""

    phase_name: str = Field(..., min_length=1)
    timing: str = Field(..., min_length=1)
    description: str = Field(..., min_length=1)


class CoordinatorPlanOutput(BaseModel):
    """The structured plan returned by the coordinator agent."""

    model_config = ConfigDict(
        json_schema_extra={"examples": [{"service_categories": ["Catering"]}]}
    )

    service_categories: list[str] = Field(..., min_length=1)
    budget_allocation: list[BudgetAllocationOutput] = Field(default_factory=list)
    target_vendor_types: list[str] = Field(..., min_length=1)
    proposed_timeline: list[TimelinePhaseOutput] = Field(..., min_length=3)
    rationale: str = Field(..., max_length=2000)
    identified_risks: list[RiskOutput] = Field(default_factory=list)
    missing_requirements: list[MissingRequirementOutput] = Field(default_factory=list)
    plan_completeness_score: int = Field(..., ge=0, le=100)
    validation_summary: str = Field(..., min_length=1)

    @field_validator("service_categories", "target_vendor_types")
    @classmethod
    def reject_blank_items(cls, values: list[str]) -> list[str]:
        if any(not value.strip() for value in values):
            raise ValueError("list items must not be blank")
        return values

    @model_validator(mode="after")
    def validate_budget_entries(self) -> "CoordinatorPlanOutput":
        if any(item.amount < 0 for item in self.budget_allocation):
            raise ValueError("budget amounts must be non-negative")
        return self

    def total_budget_allocated(self) -> float:
        """Return the total amount allocated across all categories."""

        return sum(item.amount for item in self.budget_allocation)

    def budget_validation_errors(self) -> list[str]:
        """Return budget issues that cannot be represented by field validation."""

        errors: list[str] = []
        if abs(sum(item.percentage_of_total for item in self.budget_allocation) - 100) > 0.01:
            errors.append("budget percentages must sum to 100")
        return errors

    @classmethod
    def get_json_schema_for_gemini(cls) -> dict[str, object]:
        """Return the JSON schema used to constrain a Gemini response."""

        return cls.model_json_schema()


CoordinatorPlanResponse = CoordinatorPlanOutput


def generate_json_schema() -> dict[str, object]:
    """Generate the coordinator schema for API or Gemini configuration."""

    return CoordinatorPlanOutput.get_json_schema_for_gemini()
