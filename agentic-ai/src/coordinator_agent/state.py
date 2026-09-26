"""State passed between coordinator graph nodes."""

from datetime import datetime, timezone
from typing import Any
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator

from .models import CoordinatorPlanResponse, MissingRequirementModel, RiskModel


class CoordinatorState(BaseModel):
    """Validated, copy-on-write state for one coordinator execution."""

    model_config = ConfigDict(validate_assignment=True, arbitrary_types_allowed=True)

    event_id: UUID
    event: dict[str, Any] = Field(default_factory=dict)
    event_snapshot: dict[str, Any] = Field(default_factory=dict)
    requirements_analysis: str = ""
    service_categories: list[str] = Field(default_factory=list)
    budget_allocation: dict[str, float] = Field(default_factory=dict)
    target_vendor_types: list[str] = Field(default_factory=list)
    proposed_timeline: dict[str, str] = Field(default_factory=dict)
    rationale: str = ""
    identified_risks: list[RiskModel] = Field(default_factory=list)
    missing_requirements: list[MissingRequirementModel] = Field(default_factory=list)
    completeness_score: int = Field(default=0, ge=0, le=100)
    validation_errors: list[str] = Field(default_factory=list)
    validation_passed: bool = False
    iteration_count: int = Field(default=0, ge=0)
    max_iterations: int = Field(default=3, ge=1)
    final_plan: CoordinatorPlanResponse | None = None
    generated_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))

    @field_validator("event_id", mode="before")
    @classmethod
    def validate_event_id(cls, value: object) -> UUID:
        try:
            return value if isinstance(value, UUID) else UUID(str(value))
        except (TypeError, ValueError) as exc:
            raise ValueError("event_id must be a valid UUID") from exc

    def increment_iteration(self) -> int:
        """Advance the retry counter, raising when the retry budget is exhausted."""

        if self.iteration_count >= self.max_iterations:
            raise ValueError("maximum coordinator iterations reached")
        self.iteration_count += 1
        return self.iteration_count

    def add_validation_error(self, error: str) -> None:
        """Record a non-empty validation error."""

        if not error.strip():
            raise ValueError("validation error must not be blank")
        self.validation_errors.append(error)
        self.validation_passed = False

    def is_valid_for_persistence(self) -> bool:
        """Return whether the state contains the minimum complete plan data."""

        return bool(
            self.requirements_analysis.strip()
            and self.service_categories
            and self.target_vendor_types
            and self.proposed_timeline
            and self.final_plan is not None
            and self.validation_passed
            and not self.validation_errors
        )

    def to_coordinator_plan_response(self) -> CoordinatorPlanResponse:
        """Convert state plan fields into the structured response DTO."""

        if self.final_plan is None:
            raise ValueError("final_plan is not available")
        return self.final_plan
