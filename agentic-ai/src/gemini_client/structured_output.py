"""Validation and recovery helpers for Gemini structured responses."""

import logging
from typing import Any, TypeVar

from pydantic import BaseModel, ValidationError

logger = logging.getLogger(__name__)
ModelT = TypeVar("ModelT", bound=BaseModel)


class StructuredOutputValidator:
    """Validate model output and report actionable field-level errors."""

    def validate(
        self, response: dict[str, Any], schema: type[ModelT]
    ) -> tuple[bool, list[str]]:
        try:
            schema.model_validate(response)
        except ValidationError as exc:
            errors = [
                f"{'.'.join(str(part) for part in error['loc'])}: {error['msg']}"
                for error in exc.errors()
            ]
            logger.warning("Gemini response validation failed: %s", errors)
            return False, errors
        return True, []

    def validate_with_recovery(
        self, response: dict[str, Any], schema: type[ModelT]
    ) -> ModelT:
        """Trim string values before one explicit, non-silent validation retry."""

        recovered = self._trim_strings(response)
        try:
            return schema.model_validate(recovered)
        except ValidationError as exc:
            errors = "; ".join(
                f"{error['loc']}: {error['msg']}" for error in exc.errors()
            )
            raise ValueError(f"structured response is invalid: {errors}") from exc

    def validate_budget_allocation(
        self, allocation: dict[str, float], max_budget: float
    ) -> tuple[bool, list[str]]:
        errors: list[str] = []
        if max_budget < 0:
            errors.append("max_budget must be non-negative")
        total = sum(allocation.values())
        if total > max_budget:
            errors.append(f"total allocation {total} exceeds budget {max_budget}")
        for category, amount in allocation.items():
            if not category.strip():
                errors.append("budget category must not be blank")
            if amount <= 0:
                errors.append(f"{category}: amount must be positive")
        return not errors, errors

    def validate_completeness_score(self, score: int) -> bool:
        return 0 <= score <= 100

    def validate_risks(self, risks: list[dict[str, Any]]) -> list[str]:
        return self._validate_required_items(risks, ("risk", "severity", "recommendation"), "risk")

    def validate_missing_requirements(self, requirements: list[dict[str, Any]]) -> list[str]:
        return self._validate_required_items(requirements, ("requirement", "reason"), "missing requirement")

    def validate_service_categories(self, categories: list[str]) -> bool:
        return bool(categories) and all(category.strip() for category in categories)

    def validate_timeline(self, timeline: dict[str, str]) -> bool:
        return len(timeline) >= 3 and all(key.strip() and value.strip() for key, value in timeline.items())

    @staticmethod
    def _trim_strings(value: Any) -> Any:
        if isinstance(value, str):
            return value.strip()
        if isinstance(value, dict):
            return {key: StructuredOutputValidator._trim_strings(item) for key, item in value.items()}
        if isinstance(value, list):
            return [StructuredOutputValidator._trim_strings(item) for item in value]
        return value

    @staticmethod
    def _validate_required_items(
        items: list[dict[str, Any]], fields: tuple[str, ...], label: str
    ) -> list[str]:
        errors: list[str] = []
        for index, item in enumerate(items):
            for field in fields:
                if not isinstance(item.get(field), str) or not item[field].strip():
                    errors.append(f"{label}[{index}].{field} is required")
        return errors


def coordinator_schema() -> dict[str, Any]:
    """Return the JSON schema accepted by the coordinator response model."""

    from src.coordinator_agent.models import CoordinatorPlanOutput

    return CoordinatorPlanOutput.get_json_schema_for_gemini()
