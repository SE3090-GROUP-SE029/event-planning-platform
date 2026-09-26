"""Small coordinator helpers."""

from copy import deepcopy
from typing import Any


def snapshot_event(event: dict[str, Any]) -> dict[str, Any]:
    """Capture an isolated event snapshot at plan-generation start."""

    return deepcopy(event)
