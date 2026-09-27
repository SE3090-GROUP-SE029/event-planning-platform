import asyncio
import logging
from uuid import uuid4

import pytest

from src.coordinator_agent import execution
from src.coordinator_agent.execution import (
    CoordinatorValidationError,
    execute_coordinator_agent,
)
from src.coordinator_agent.models import RiskModel
from src.coordinator_agent.nodes.validate import (
    calculate_completeness,
    self_validate,
)
from src.coordinator_agent.state import CoordinatorState


def _valid_state() -> CoordinatorState:
    return CoordinatorState(
        event_id=uuid4(),
        event={
            "type": "WEDDING",
            "date": "2027-06-15T18:00:00",
            "guest_count": 100,
            "budget": 1000,
        },
        requirements_analysis="The event needs catering, a venue, and photography.",
        service_categories=["Catering"],
        target_vendor_types=["Catering provider"],
        budget_allocation={"Catering": 1000.0},
        proposed_timeline={
            "Planning": "12 weeks before",
            "Booking": "8 weeks before",
            "Confirmation": "1 week before",
        },
        rationale=(
            "Catering is prioritized for the guest count, while the schedule "
            "leaves enough time to confirm details before the event."
        ),
        identified_risks=[
            RiskModel(
                risk="A supplier may become unavailable.",
                severity="Medium",
                recommendation="Confirm bookings and retain a backup option.",
            )
        ],
    )


def test_self_validate_accepts_complete_budget_balanced_plan(caplog) -> None:
    caplog.set_level(logging.INFO)

    result = asyncio.run(self_validate(_valid_state()))

    assert result == {"validation_passed": True, "validation_errors": []}
    assert "Generated plan before validation" in caplog.text
    assert '"passed": true' in caplog.text
    assert '"budget_total": 1000.0' in caplog.text


def test_self_validate_reports_missing_and_invalid_plan_sections(caplog) -> None:
    state = _valid_state()
    state.event["date"] = "not-a-date"
    state.budget_allocation = {"Catering": 900.0}
    state.identified_risks = []
    state.rationale = "short"
    state.requirements_analysis = ""
    caplog.set_level(logging.ERROR)

    result = asyncio.run(self_validate(state))

    errors = result["validation_errors"]
    assert result["validation_passed"] is False
    assert "event.date must be a valid date" in errors
    assert "budget allocation total 900.00 does not match event budget 1000.00" in errors
    assert "identified_risks is empty" in errors
    assert "requirements_analysis is empty" in errors
    assert "rationale must contain at least 100 characters" in errors
    assert "Validation failed:" in caplog.text
    assert '"missing_fields": ["requirements_analysis", "identified_risks"]' in caplog.text


def test_completeness_accepts_nullable_event_requirements() -> None:
    state = _valid_state()
    state.event["requirements"] = None

    result = asyncio.run(calculate_completeness(state))

    assert 0 <= result["completeness_score"] <= 100


def test_self_validate_rejects_one_cent_budget_mismatch() -> None:
    state = _valid_state()
    state.budget_allocation = {"Catering": 999.99}

    result = asyncio.run(self_validate(state))

    assert result["validation_passed"] is False
    assert (
        "budget allocation total 999.99 does not match event budget 1000.00"
        in result["validation_errors"]
    )


def test_execution_preserves_actionable_final_validation_errors(
    monkeypatch: pytest.MonkeyPatch, caplog
) -> None:
    failed_state = _valid_state()
    failed_state.validation_errors = [
        "budget allocation total 900.00 does not match event budget 1000.00"
    ]
    failed_state.validation_passed = False

    class _FakeGraph:
        async def ainvoke(self, _initial_state: CoordinatorState) -> CoordinatorState:
            return failed_state

    monkeypatch.setattr(
        execution, "build_coordinator_graph", lambda _checkpointer: _FakeGraph()
    )

    with pytest.raises(CoordinatorValidationError, match="budget allocation total"):
        asyncio.run(
            execute_coordinator_agent(
                str(uuid4()),
                {
                    "type": "WEDDING",
                    "date": "2027-06-15T18:00:00",
                    "guest_count": 100,
                    "budget": 1000,
                },
                max_retries=1,
                timeout_seconds=5,
            )
        )

    assert "Coordinator final validation failed" in caplog.text
    assert "budget allocation total 900.00 does not match event budget 1000.00" in caplog.text
