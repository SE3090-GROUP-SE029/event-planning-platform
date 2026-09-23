"""Requirement-analysis node."""

from ..state import CoordinatorState


def analyze_requirements(state: CoordinatorState) -> CoordinatorState:
    """Return state unchanged until the LLM-backed analysis is wired in."""

    return state
