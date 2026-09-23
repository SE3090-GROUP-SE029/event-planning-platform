"""LangGraph definition for the coordinator workflow."""

from typing import Any

from langgraph.graph import END, START, StateGraph

from .state import CoordinatorState
from .nodes.analyze import analyze_requirements
from .nodes.assess import allocate_budget
from .nodes.detect import assess_risks
from .nodes.generate import identify_service_categories
from .nodes.rationale import generate_rationale
from .nodes.score import detect_missing_requirements
from .nodes.timeline import propose_timeline
from .nodes.validate import calculate_completeness, self_validate


def build_coordinator_graph() -> Any:
    """Build a typed graph ready for LLM-backed node implementations."""

    graph = StateGraph(CoordinatorState)
    graph.add_node("analyze", analyze_requirements)
    graph.add_node("categories", identify_service_categories)
    graph.add_node("budget", allocate_budget)
    graph.add_node("timeline", propose_timeline)
    graph.add_node("risks", assess_risks)
    graph.add_node("missing_requirements", detect_missing_requirements)
    graph.add_node("rationale", generate_rationale)
    graph.add_node("score", calculate_completeness)
    graph.add_node("validate", self_validate)
    graph.add_edge(START, "analyze")
    graph.add_edge("analyze", "categories")
    graph.add_edge("categories", "budget")
    graph.add_edge("budget", "timeline")
    graph.add_edge("timeline", "risks")
    graph.add_edge("risks", "missing_requirements")
    graph.add_edge("missing_requirements", "rationale")
    graph.add_edge("rationale", "score")
    graph.add_edge("score", "validate")
    graph.add_edge("validate", END)
    return graph.compile()
