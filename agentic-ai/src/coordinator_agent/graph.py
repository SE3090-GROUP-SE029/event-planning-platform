"""LangGraph workflow assembly for the coordinator agent."""

import json
import logging
from typing import Any

from langgraph.checkpoint.base import BaseCheckpointSaver
from langgraph.graph import END, START, StateGraph
from pydantic import ValidationError

from .models import (
    CoordinatorPlanResponse,
)
from .nodes.analyze import analyze_requirements
from .nodes.assess import allocate_budget
from .nodes.detect import assess_risks
from .nodes.generate import identify_service_categories
from .nodes.rationale import generate_rationale
from .nodes.score import detect_missing_requirements
from .nodes.timeline import propose_timeline
from .nodes.validate import calculate_completeness, self_validate
from .state import CoordinatorState
from .utils import snapshot_event

logger = logging.getLogger(__name__)


def load_event_context(state: CoordinatorState) -> dict[str, object]:
    """Capture an isolated event snapshot before any planning node runs."""

    return {"event_snapshot": snapshot_event(state.event)}


def finalize_plan(state: CoordinatorState) -> dict[str, object]:
    """Build the persistence DTO from validated coordinator state."""

    budget_total = sum(state.budget_allocation.values())
    budget_items = [
        {
            "category": category,
            "amount": amount,
            "percentage_of_total": amount / budget_total * 100,
        }
        for category, amount in state.budget_allocation.items()
    ]
    candidate = {
        "service_categories": state.service_categories,
        "budget_allocation": budget_items,
        "target_vendor_types": state.target_vendor_types,
        "proposed_timeline": [
            {
                "phase_name": phase,
                "timing": timing,
                "description": f"Complete {phase.lower()} for the event.",
            }
            for phase, timing in state.proposed_timeline.items()
        ],
        "rationale": state.rationale,
        "identified_risks": [
            risk.model_dump(mode="json") for risk in state.identified_risks
        ],
        "missing_requirements": [
            item.model_dump(mode="json") for item in state.missing_requirements
        ],
        "plan_completeness_score": state.completeness_score,
        "validation_summary": "Coordinator self-validation passed.",
    }
    logger.info(
        "Generated plan before validation: %s",
        json.dumps(candidate, ensure_ascii=False, sort_keys=True),
    )
    try:
        plan = CoordinatorPlanResponse.model_validate(candidate)
    except ValidationError as exc:
        logger.error(
            "Validation failed: %s",
            json.dumps(
                exc.errors(include_input=False, include_url=False),
                ensure_ascii=False,
                sort_keys=True,
            ),
        )
        raise
    return {"final_plan": plan}


def handle_validation_error(state: CoordinatorState) -> dict[str, object]:
    """Mark terminal validation failure without hiding the collected errors."""

    logger.error(
        "Coordinator validation failed after %d iteration(s): %s",
        state.iteration_count,
        state.validation_errors,
    )
    return {"validation_passed": False}


def _route_after_validation(state: CoordinatorState) -> str:
    if state.validation_passed:
        return "finalize_plan"
    if state.iteration_count < state.max_iterations - 1:
        state.increment_iteration()
        logger.info("Retrying coordinator workflow: %d/%d", state.iteration_count, state.max_iterations)
        return "analyze_requirements"
    return "handle_validation_error"


def build_coordinator_graph(
    checkpointer: BaseCheckpointSaver | None = None,
) -> Any:
    """Build the sequential, retry-aware coordinator graph."""

    graph = StateGraph(CoordinatorState)
    graph.add_node("load_event_context", load_event_context)
    graph.add_node("analyze_requirements", analyze_requirements)
    graph.add_node("identify_service_categories", identify_service_categories)
    graph.add_node("propose_timeline", propose_timeline)
    graph.add_node("allocate_budget", allocate_budget)
    graph.add_node("assess_risks", assess_risks)
    graph.add_node("detect_missing_requirements", detect_missing_requirements)
    graph.add_node("calculate_completeness", calculate_completeness)
    graph.add_node("generate_rationale", generate_rationale)
    graph.add_node("self_validate", self_validate)
    graph.add_node("finalize_plan", finalize_plan)
    graph.add_node("handle_validation_error", handle_validation_error)

    graph.add_edge(START, "load_event_context")
    graph.add_edge("load_event_context", "analyze_requirements")
    graph.add_edge("analyze_requirements", "identify_service_categories")
    graph.add_edge("identify_service_categories", "propose_timeline")
    graph.add_edge("propose_timeline", "allocate_budget")
    graph.add_edge("allocate_budget", "assess_risks")
    graph.add_edge("assess_risks", "detect_missing_requirements")
    graph.add_edge("detect_missing_requirements", "calculate_completeness")
    graph.add_edge("calculate_completeness", "generate_rationale")
    graph.add_edge("generate_rationale", "self_validate")
    graph.add_conditional_edges(
        "self_validate",
        _route_after_validation,
        {
            "finalize_plan": "finalize_plan",
            "analyze_requirements": "analyze_requirements",
            "handle_validation_error": "handle_validation_error",
        },
    )
    graph.add_edge("finalize_plan", END)
    graph.add_edge("handle_validation_error", END)
    return graph.compile(checkpointer=checkpointer)
