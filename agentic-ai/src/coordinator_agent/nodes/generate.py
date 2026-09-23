"""Plan-generation node."""

from ..state import CoordinatorState


def generate_plan(state: CoordinatorState) -> CoordinatorState:
    """Return state unchanged until the LLM-backed generation is wired in."""

    return state
