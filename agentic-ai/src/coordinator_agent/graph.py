"""LangGraph definition for the coordinator workflow."""

from typing import Any

from langgraph.graph import END, START, StateGraph

from .state import CoordinatorState


def build_coordinator_graph() -> Any:
    """Build a typed graph ready for LLM-backed node implementations."""

    graph = StateGraph(CoordinatorState)
    for name in ("analyze", "generate", "assess", "detect", "score", "validate"):
        graph.add_node(name, lambda state: state)
    graph.add_edge(START, "analyze")
    graph.add_edge("analyze", "generate")
    graph.add_edge("generate", "assess")
    graph.add_edge("assess", "detect")
    graph.add_edge("detect", "score")
    graph.add_edge("score", "validate")
    graph.add_edge("validate", END)
    return graph.compile()
