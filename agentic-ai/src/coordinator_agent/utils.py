"""Small coordinator helpers."""

from copy import deepcopy
import re
from typing import Any


def normalize_service_category(category: str) -> str:
    """Normalize category names for matching generated planning artifacts."""

    return re.sub(r"[^a-z0-9]+", " ", category.casefold()).strip()


def snapshot_event(event: dict[str, Any]) -> dict[str, Any]:
    """Capture an isolated event snapshot at plan-generation start."""

    return deepcopy(event)
