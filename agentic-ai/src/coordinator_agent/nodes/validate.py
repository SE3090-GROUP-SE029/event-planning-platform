"""Completeness scoring node."""

import json
import logging
import math
from datetime import date, datetime
from typing import Any

from pydantic import BaseModel

from ..state import CoordinatorState
from ..utils import normalize_service_category

logger = logging.getLogger(__name__)


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
        + (
            5
            if isinstance(event.get("requirements"), str)
            and len(event["requirements"].strip()) >= 3
            else 0
        )
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
    for key in ("type", "date", "guest_count", "budget"):
        value = state.event.get(key)
        if value is None or value == "":
            errors.append(f"event.{key} is required")
    if state.event.get("type") not in {"WEDDING", "CORPORATE", "BIRTHDAY"}:
        errors.append("event.type is not a supported event type")

    event_date = state.event.get("date")
    if event_date is not None and not _is_valid_event_date(event_date):
        errors.append("event.date must be a valid date")

    guest_count = state.event.get("guest_count")
    if isinstance(guest_count, bool) or not isinstance(guest_count, int) or guest_count <= 0:
        errors.append("event.guest_count must be a positive integer")

    event_budget = _finite_number(state.event.get("budget"))
    if event_budget is None or event_budget <= 0:
        errors.append("event.budget must be a finite positive amount")

    if not state.service_categories:
        errors.append("service_categories is empty")
    elif any(not category.strip() for category in state.service_categories):
        errors.append("service_categories contains a blank category")
    if len(
        {normalize_service_category(category) for category in state.service_categories}
    ) != len(state.service_categories):
        errors.append("service_categories contains duplicate normalized categories")
    if not state.budget_allocation:
        errors.append("budget_allocation is empty")
    if not state.target_vendor_types:
        errors.append("target_vendor_types is empty")
    elif any(not vendor_type.strip() for vendor_type in state.target_vendor_types):
        errors.append("target_vendor_types contains a blank vendor type")
    if state.requirements_analysis.strip() == "":
        errors.append("requirements_analysis is empty")
    if len(state.target_vendor_types) != len(state.service_categories):
        errors.append("target_vendor_types must map one-to-one to service_categories")
    if len(state.proposed_timeline) < 3:
        errors.append("proposed_timeline must contain at least 3 phases")
    if any(
        not isinstance(phase, str)
        or not phase.strip()
        or not isinstance(timing, str)
        or not timing.strip()
        for phase, timing in state.proposed_timeline.items()
    ):
        errors.append("proposed_timeline contains an invalid phase or timing")
    if not state.identified_risks:
        errors.append("identified_risks is empty")
    for index, risk in enumerate(state.identified_risks):
        if not risk.risk.strip() or not risk.recommendation.strip():
            errors.append(f"identified_risks[{index}] has a missing required field")
    for index, requirement in enumerate(state.missing_requirements):
        if not requirement.requirement.strip() or not requirement.reason.strip():
            errors.append(
                f"missing_requirements[{index}] has a missing required field"
            )
    if len(state.rationale.strip()) < 100:
        errors.append("rationale must contain at least 100 characters")
    total = sum(state.budget_allocation.values())
    if event_budget is not None and event_budget > 0 and not math.isclose(
        total, event_budget, rel_tol=0, abs_tol=0.000001
    ):
        errors.append(
            f"budget allocation total {total:.2f} does not match event budget "
            f"{event_budget:.2f}"
        )
    if any(
        not math.isfinite(value) or value <= 0
        for value in state.budget_allocation.values()
    ):
        errors.append("all budget allocations must be positive")
    if set(state.service_categories) - set(state.budget_allocation):
        errors.append("service categories and budget allocation keys do not align")
    unexpected_budget_categories = set(state.budget_allocation) - (
        set(state.service_categories) | {"Contingency"}
    )
    if unexpected_budget_categories:
        errors.append(
            "budget_allocation contains unknown categories: "
            + ", ".join(sorted(unexpected_budget_categories))
        )

    passed = not errors
    report = {
        "event_requirements": {
            "event_type": state.event.get("type"),
            "event_size": state.event.get("guest_count"),
            "budget": state.event.get("budget"),
            "date": _json_value(state.event.get("date")),
        },
        "requirements_analysis": state.requirements_analysis,
        "service_categories": state.service_categories,
        "target_vendor_types": state.target_vendor_types,
        "budget_allocation": state.budget_allocation,
        "budget_total": total,
        "budget_allocation_percentages": {
            category: (amount / total * 100 if total else 0)
            for category, amount in state.budget_allocation.items()
        },
        "timeline": state.proposed_timeline,
        "risk_assessments": [
            risk.model_dump(mode="json") for risk in state.identified_risks
        ],
        "missing_requirements": [
            item.model_dump(mode="json") for item in state.missing_requirements
        ],
        "rationale": state.rationale,
        "plan_completeness_score": state.completeness_score,
        "validation_result": {"passed": passed, "errors": errors},
    }
    logger.info(
        "Generated plan before validation: %s",
        json.dumps(report, ensure_ascii=False, sort_keys=True),
    )
    if errors:
        missing_errors = [
            error
            for error in errors
            if " is required" in error or " is empty" in error
        ]
        missing_fields = [
            error.split(" is ", maxsplit=1)[0] for error in missing_errors
        ]
        invalid_fields = [error for error in errors if error not in missing_errors]
        logger.error(
            "Validation failed: %s",
            json.dumps(
                {
                    "validation_errors": errors,
                    "failed_rules": errors,
                    "missing_fields": missing_fields,
                    "invalid_fields": invalid_fields,
                    "budget_total": total,
                    "budget_allocation_percentages": report[
                        "budget_allocation_percentages"
                    ],
                    "budget": state.event.get("budget"),
                    "timeline": state.proposed_timeline,
                    "service_categories": state.service_categories,
                    "risk_assessments": report["risk_assessments"],
                    "missing_requirements": report["missing_requirements"],
                    "rationale": state.rationale,
                    "plan_completeness_score": state.completeness_score,
                },
                ensure_ascii=False,
                sort_keys=True,
            ),
        )
    else:
        logger.info(
            "Coordinator validation result: passed budget_total=%.2f budget=%.2f",
            total,
            event_budget or 0,
        )
    return {"validation_passed": passed, "validation_errors": errors}


async def validate_plan(state: CoordinatorState) -> dict[str, object]:
    """Backward-compatible alias for self-validation."""

    return await self_validate(state)


def _finite_number(value: Any) -> float | None:
    if isinstance(value, bool):
        return None
    try:
        result = float(value)
    except (TypeError, ValueError):
        return None
    return result if math.isfinite(result) else None


def _is_valid_event_date(value: Any) -> bool:
    if isinstance(value, (date, datetime)):
        return True
    if not isinstance(value, str) or not value.strip():
        return False
    try:
        datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        try:
            date.fromisoformat(value)
        except ValueError:
            return False
    return True


def _json_value(value: Any) -> Any:
    if isinstance(value, BaseModel):
        return value.model_dump(mode="json")
    if isinstance(value, (date, datetime)):
        return value.isoformat()
    return value
