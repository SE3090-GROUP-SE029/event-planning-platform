"""Completeness scoring node."""

from ..state import CoordinatorState


def _present(event: dict[str, object], key: str) -> bool:
    value = event.get(key)
    return value is not None and value != "" and value != []


async def calculate_completeness(state: CoordinatorState) -> dict[str, object]:
    """Calculate a deterministic 0-100 completeness score."""

    event = state.event
    core = sum(5 for key in ("name", "date", "location", "guest_count", "budget") if _present(event, key))
    clarity = (
        (5 if _present(event, "type") else 0)
        + (5 if bool(state.requirements_analysis.strip()) else 0)
        + (5 if len(event.get("requirements", [])) >= 3 else 0)
        + (5 if _present(event, "special_requests") else 0)
    )
    artifacts = (
        (10 if state.service_categories else 0)
        + (10 if state.budget_allocation else 0)
        + (10 if len(state.proposed_timeline) >= 3 else 0)
    )
    risk_score = max(0, 25 - len(state.identified_risks) * 2 - len(state.missing_requirements) * 2)
    breakdown = {
        "core_event_data": core,
        "requirement_clarity": clarity,
        "planning_artifacts": artifacts,
        "risk_and_conflicts": risk_score,
    }
    return {"completeness_score": max(0, min(100, sum(breakdown.values()))), "score_breakdown": breakdown}


async def self_validate(state: CoordinatorState) -> dict[str, object]:
    """Validate the generated plan and collect all actionable errors."""

    errors: list[str] = []
    if not state.service_categories:
        errors.append("service_categories is empty")
    if not state.budget_allocation:
        errors.append("budget_allocation is empty")
    if not state.target_vendor_types:
        errors.append("target_vendor_types is empty")
    if len(state.proposed_timeline) < 3:
        errors.append("proposed_timeline must contain at least 3 phases")
    if len(state.rationale.strip()) < 100:
        errors.append("rationale must contain at least 100 characters")
    event_budget = float(state.event.get("budget", 0) or 0)
    total = sum(state.budget_allocation.values())
    if total > event_budget:
        errors.append(f"budget allocation {total} exceeds event budget {event_budget}")
    if any(value <= 0 for value in state.budget_allocation.values()):
        errors.append("all budget allocations must be positive")
    if set(state.service_categories) - set(state.budget_allocation):
        errors.append("service categories and budget allocation keys do not align")
    passed = not errors
    return {"validation_passed": passed, "validation_errors": errors}


async def validate_plan(state: CoordinatorState) -> dict[str, object]:
    """Backward-compatible alias for self-validation."""

    return await self_validate(state)
