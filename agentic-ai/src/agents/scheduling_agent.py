from datetime import datetime
from typing import List

from langgraph.graph import END, START, StateGraph
from pydantic import BaseModel

from src.gemini_client.client import GeminiClient
from src.models.scheduling_models import (
    ActivityItem,
    GenerateScheduleResponse,
    ScheduleState,
)

SYSTEM_PROMPT = """
You are an expert AI Event Scheduling Agent.
Given the event operational window and details, construct a logical run-of-show sequence.
Rules:
1. Setup and audio/visual checks must be scheduled before guests arrive.
2. Main events/sessions must be ordered chronologically with realistic buffer times.
3. Teardown/cleanup must conclude the event.
4. Assign appropriate vendor categories (AudioVisual, Catering, Photography, Hospitality).
"""


class GeneratedList(BaseModel):
    activities: List[ActivityItem]


class SchedulingAgent:
    def __init__(self, gemini_client: GeminiClient):
        self.client = gemini_client
        self.workflow = self._build_graph()

    def _build_graph(self):
        graph = StateGraph(ScheduleState)

        # Node 1: LLM Run-of-Show Generation
        async def generate_node(state: ScheduleState):
            user_prompt = f"""
            Plan an execution schedule:
            Event: {state.title} ({state.event_type})
            Date: {state.date}
            Window: {state.start_time} to {state.end_time}
            Guests: {state.guest_count}
            Notes: {state.requirements or 'Standard setup'}
            """

            result = await self.client.generate_structured_output(
                system_prompt=SYSTEM_PROMPT,
                user_prompt=user_prompt,
                response_schema=GeneratedList,
            )
            return {"raw_activities": result.activities}

        # Node 2: Deterministic Business Rule & Conflict Detection (Allow-listed Tool Check)
        def validate_and_detect_conflicts(state: ScheduleState):
            validated = []
            conflicts = []
            sorted_acts = sorted(state.raw_activities, key=lambda x: x.start_time)

            for i, act in enumerate(sorted_acts):
                # Verify ISO timestamp formats
                try:
                    s = datetime.fromisoformat(act.start_time.replace("Z", "+00:00"))
                    e = datetime.fromisoformat(act.end_time.replace("Z", "+00:00"))
                    if e <= s:
                        conflicts.append(f"Invalid duration: {act.title} ends before or when it starts.")
                        continue
                except ValueError:
                    continue

                # Vendor overlap conflict check
                for j in range(i + 1, len(sorted_acts)):
                    next_act = sorted_acts[j]
                    if act.vendor_type == next_act.vendor_type:
                        next_s = datetime.fromisoformat(next_act.start_time.replace("Z", "+00:00"))
                        if next_s < e:
                            conflicts.append(
                                f"Vendor clash ({act.vendor_type}): '{act.title}' overlaps with '{next_act.title}'."
                            )

                validated.append(act)

            return {"validated_activities": validated, "conflicts": conflicts}

        graph.add_node("generate", generate_node)
        graph.add_node("validate", validate_and_detect_conflicts)

        graph.add_edge(START, "generate")
        graph.add_edge("generate", "validate")
        graph.add_edge("validate", END)

        return graph.compile()

    async def run(self, state: ScheduleState) -> GenerateScheduleResponse:
        final_state = await self.workflow.ainvoke(state)
        return GenerateScheduleResponse(
            activities=final_state["validated_activities"],
            conflicts=final_state["conflicts"],
        )