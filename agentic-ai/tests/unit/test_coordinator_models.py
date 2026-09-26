from uuid import uuid4

import pytest
from pydantic import ValidationError

from src.coordinator_agent.models import (
    BudgetAllocationOutput,
    CoordinatorPlanOutput,
    TimelinePhaseOutput,
)
from src.coordinator_agent.state import CoordinatorState
from src.gemini_client.structured_output import StructuredOutputValidator


def test_state_validates_uuid_and_iteration() -> None:
    state = CoordinatorState(event_id=str(uuid4()), max_iterations=2)
    assert state.increment_iteration() == 1


def test_state_rejects_invalid_score() -> None:
    with pytest.raises(ValidationError):
        CoordinatorState(event_id=uuid4(), completeness_score=101)


def test_plan_schema_and_budget_helpers() -> None:
    plan = CoordinatorPlanOutput(
        service_categories=["Catering"],
        budget_allocation=[
            BudgetAllocationOutput(
                category="Catering", amount=100.0, percentage_of_total=100.0
            )
        ],
        target_vendor_types=["Caterer"],
        proposed_timeline=[
            TimelinePhaseOutput(phase_name="A", timing="Now", description="Do A"),
            TimelinePhaseOutput(phase_name="B", timing="Later", description="Do B"),
            TimelinePhaseOutput(phase_name="C", timing="Event", description="Do C"),
        ],
        rationale="A complete plan",
        plan_completeness_score=90,
        validation_summary="Valid",
    )
    assert plan.total_budget_allocated() == 100.0
    assert plan.get_json_schema_for_gemini()["type"] == "object"


def test_structured_validator_reports_budget_errors() -> None:
    valid, errors = StructuredOutputValidator().validate_budget_allocation(
        {"Catering": 110.0, "Venue": 0.0}, 100.0
    )
    assert not valid
    assert "total allocation 110.0 exceeds budget 100.0" in errors
    assert "Venue: amount must be positive" in errors
