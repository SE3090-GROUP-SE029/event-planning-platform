"""Self-validation node."""

from ..state import CoordinatorState


def validate_plan(state: CoordinatorState) -> CoordinatorState:
    """Set validation status from the state persistence checks."""

    state.validation_passed = state.is_valid_for_persistence()
    return state
