"""Missing-requirements node."""

from ..state import CoordinatorState


def detect_missing_requirements(state: CoordinatorState) -> CoordinatorState:
    """Return state unchanged until requirement detection is wired in."""

    return state
