"""Requirement-analysis node."""

import json
import logging

from pydantic import BaseModel, Field

from src.coordinator_agent.prompts import REQUIREMENTS_ANALYSIS_PROMPT
from src.gemini_client.client import GeminiClient
from ..state import CoordinatorState

logger = logging.getLogger(__name__)


class AnalysisOutput(BaseModel):
    analysis: str = Field(..., min_length=1, max_length=4000)


async def analyze_requirements(state: CoordinatorState) -> dict[str, str]:
    """Analyze event requirements and return only the analysis update."""

    logger.info("Analyzing requirements for event %s", state.event_id)
    prompt = REQUIREMENTS_ANALYSIS_PROMPT.format(
        event_details=json.dumps(state.event, ensure_ascii=True, default=str)
    )
    try:
        result = await GeminiClient().generate_with_prompt(prompt, AnalysisOutput)
        analysis = result.analysis.strip()
        if not analysis:
            raise ValueError("Gemini returned an empty requirements analysis")
        logger.info("Requirements analysis complete for event %s", state.event_id)
        return {"requirements_analysis": analysis[:2000]}
    except TimeoutError:
        logger.exception("Requirements analysis timed out for event %s", state.event_id)
        raise
    except Exception:
        logger.exception("Requirements analysis failed for event %s", state.event_id)
        raise
