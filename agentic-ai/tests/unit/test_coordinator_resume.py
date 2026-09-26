import asyncio
from uuid import uuid4

import pytest
from langgraph.checkpoint.sqlite.aio import AsyncSqliteSaver

from src.coordinator_agent import graph
from src.coordinator_agent.execution import (
    CoordinatorProviderError,
    execute_coordinator_agent,
)
from src.gemini_client.exceptions import GeminiQuotaError


def test_coordinator_resumes_after_quota_failure_without_repeating_completed_nodes(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setenv("LANGGRAPH_STRICT_MSGPACK", "true")
    calls = {
        "analysis": 0,
        "categories": 0,
        "timeline": 0,
        "budget": 0,
        "risks": 0,
        "requirements": 0,
        "rationale": 0,
    }

    async def analyze(_state: object) -> dict[str, str]:
        calls["analysis"] += 1
        return {"requirements_analysis": "Event requirements are understood."}

    async def categorize(_state: object) -> dict[str, list[str]]:
        calls["categories"] += 1
        return {
            "service_categories": ["Catering"],
            "target_vendor_types": ["Catering provider"],
        }

    async def timeline(_state: object) -> dict[str, dict[str, str]]:
        calls["timeline"] += 1
        return {
            "proposed_timeline": {
                "Planning": "12 weeks before",
                "Booking": "8 weeks before",
                "Confirmation": "1 week before",
            }
        }

    async def allocate(_state: object) -> dict[str, dict[str, float]]:
        calls["budget"] += 1
        if calls["budget"] == 1:
            raise GeminiQuotaError("quota exhausted")
        return {"budget_allocation": {"Catering": 900.0, "Contingency": 100.0}}

    async def assess(_state: object) -> dict[str, list[object]]:
        calls["risks"] += 1
        return {"identified_risks": []}

    async def detect(_state: object) -> dict[str, list[object]]:
        calls["requirements"] += 1
        return {"missing_requirements": []}

    async def rationale(_state: object) -> dict[str, str]:
        calls["rationale"] += 1
        return {
            "rationale": (
                "The selected categories, allocations, and timeline fit the event "
                "budget, guest count, and stated planning constraints."
            )
        }

    monkeypatch.setattr(graph, "analyze_requirements", analyze)
    monkeypatch.setattr(graph, "identify_service_categories", categorize)
    monkeypatch.setattr(graph, "propose_timeline", timeline)
    monkeypatch.setattr(graph, "allocate_budget", allocate)
    monkeypatch.setattr(graph, "assess_risks", assess)
    monkeypatch.setattr(graph, "detect_missing_requirements", detect)
    monkeypatch.setattr(graph, "generate_rationale", rationale)

    async def execute_with_checkpoint() -> None:
        async with AsyncSqliteSaver.from_conn_string(":memory:") as checkpointer:
            await checkpointer.setup()
            event_id = str(uuid4())
            event = {"name": "Gala", "budget": 1000, "guest_count": 100}

            with pytest.raises(CoordinatorProviderError) as error:
                await execute_coordinator_agent(
                    event_id,
                    event,
                    checkpointer=checkpointer,
                )
            assert error.value.code == "quota_exhausted"
            assert calls["analysis"] == 1
            assert calls["categories"] == 1
            assert calls["timeline"] == 1

            result = await execute_coordinator_agent(
                event_id,
                event,
                checkpointer=checkpointer,
            )

            assert result.service_categories == ["Catering"]
            assert calls["analysis"] == 1
            assert calls["categories"] == 1
            assert calls["timeline"] == 1
            assert calls["budget"] == 2
            assert calls["risks"] == 1
            assert calls["requirements"] == 1
            assert calls["rationale"] == 1

    asyncio.run(execute_with_checkpoint())
