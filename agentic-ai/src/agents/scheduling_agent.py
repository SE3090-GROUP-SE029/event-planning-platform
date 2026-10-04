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
Given the event date, local wall-clock time window, title, type, and description,
construct a logical run-of-show sequence that fits inside the event window.

Rules:
1. Generate activities that are semantically related to the actual event.
2. Use the title, event type, and description to decide what belongs in the timeline.
3. Birthday examples include guest arrival, entertainment/games, dinner, cake cutting, photography, and music/social time.
4. Wedding examples include guest arrival, ceremony, photography, reception, dinner, cake cutting, and entertainment.
5. Conference examples include registration, opening, keynote, sessions, networking, and closing.
6. These examples are guidance, not fixed lists or mandatory labels.
7. Use realistic durations based on the activity type and total available time.
8. Do not divide the whole window into equal fixed blocks.
9. If everything cannot fit, generate fewer high-value activities.
10. Never place an activity before the event start or after the event end.
11. Every activity must be on the supplied event date.
12. Return local event-date ISO timestamps without timezone, for example 2027-01-01T18:00:00.
13. Assign appropriate vendor categories, such as AudioVisual, Catering, Photography, Hospitality, Entertainment, or Coordination.
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
            Description: {state.event_description or state.requirements or 'No additional description'}
            Date: {state.date}
            Local event window: {state.start_time} to {state.end_time}
            Guests: {state.guest_count}
            Vendor/service context: {state.vendor_service_context or 'No booked vendor context supplied'}
            Notes: {state.requirements or 'Standard setup'}

            Duration guidance:
            - welcome/arrival: about 30-60 minutes
            - short speech/opening: about 10-30 minutes
            - cake cutting: about 30-60 minutes
            - meal: about 60-90 minutes
            - photography: about 30-60 minutes
            - entertainment/dancing: about 60-120 minutes
            """

            prompt = f"{SYSTEM_PROMPT.strip()}\n\n{user_prompt.strip()}"
            result = await self.client.generate_with_prompt(
                prompt,
                GeneratedList,
                node_name="generate_schedule",
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
