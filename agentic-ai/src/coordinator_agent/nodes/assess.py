"""Risk-assessment node."""

from ..state import CoordinatorState


def assess_risks(state: CoordinatorState) -> CoordinatorState:
    """Return state unchanged until risk assessment is wired in."""

    return state
