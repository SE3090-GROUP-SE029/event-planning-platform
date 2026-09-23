"""Completeness-scoring node."""

from ..state import CoordinatorState


def score_completeness(state: CoordinatorState) -> CoordinatorState:
    """Return state unchanged until scoring is wired in."""

    return state
