import asyncio
import json
import logging
import os
import re
from collections.abc import Awaitable, Callable
from contextlib import aclosing
from dataclasses import dataclass
from urllib.parse import urlsplit
from uuid import uuid4

from pydantic import ValidationError

from src.models.guest_review_models import GuestDecision, GuestReviewRequest, GuestReviewResponse

logger = logging.getLogger(__name__)


class AnalysisError(Exception):
    def __init__(self, code: str, status_code: int):
        super().__init__(code)
        self.code = code
        self.status_code = status_code


@dataclass(frozen=True)
class ReviewSettings:
    model: str = "qwen3:8b"
    ollama_url: str = "http://127.0.0.1:11434"
    timeout_seconds: float = 110

    @classmethod
    def from_environment(cls):
        try:
            settings = cls(os.getenv("OLLAMA_MODEL", "qwen3:8b"),
                           os.getenv("OLLAMA_API_BASE", "http://127.0.0.1:11434"),
                           float(os.getenv("GUEST_AI_TIMEOUT_SECONDS", "110")))
            settings.validate()
            return settings
        except (ValueError, TypeError):
            raise AnalysisError("ai_unavailable", 503) from None

    def validate(self):
        url = urlsplit(self.ollama_url)
        if (url.scheme not in {"http", "https"} or url.hostname not in {"127.0.0.1", "localhost", "::1"}
                or url.username or url.password or url.query or url.fragment or url.path not in {"", "/"}
                or not 0 < self.timeout_seconds <= 300
                or not re.fullmatch(r"[a-zA-Z0-9_.:/-]{1,120}", self.model)
                or "cloud" in self.model.lower()):
            raise ValueError("Local Ollama configuration required")


async def run_adk(context, settings: ReviewSettings, agent_factory=None) -> str:
    # Do not fetch remote model metadata or emit payloads through provider diagnostics.
    os.environ["LITELLM_LOCAL_MODEL_COST_MAP"] = "True"
    from google.adk.agents.run_config import RunConfig
    from google.adk.models.lite_llm import LiteLlm
    from google.adk.runners import Runner
    from google.adk.sessions import InMemorySessionService
    from google.genai import types
    import litellm
    from src.agents.guest_filtering_agent import create_agent
    agent_factory = agent_factory or create_agent

    litellm.suppress_debug_info = True
    litellm.turn_off_message_logging = True
    litellm.set_verbose = False
    for name in ("google.adk", "LiteLLM", "litellm"):
        logging.getLogger(name).setLevel(logging.CRITICAL)

    model = LiteLlm(model=f"ollama_chat/{settings.model}", api_base=settings.ollama_url,
                    timeout=settings.timeout_seconds, num_retries=0, reasoning_effort="none")
    sessions = InMemorySessionService()
    session_id = uuid4().hex
    agent = agent_factory(model)
    runner = Runner(app_name=agent.name, agent=agent, session_service=sessions)
    await sessions.create_session(app_name=agent.name, user_id="analysis", session_id=session_id)
    try:
        async with aclosing(runner.run_async(
            user_id="analysis", session_id=session_id,
            new_message=types.Content(role="user", parts=[types.Part(text=context.model_dump_json(by_alias=True))]),
            run_config=RunConfig(max_llm_calls=1),
        )) as events:
            async for event in events:
                if event.is_final_response() and event.content:
                    return "".join(part.text for part in event.content.parts or [] if part.text and not part.thought)
        raise AnalysisError("invalid_ai_response", 502)
    except litellm.Timeout:
        raise AnalysisError("ai_timeout", 504) from None
    finally:
        await sessions.delete_session(app_name=agent.name, user_id="analysis", session_id=session_id)
        await runner.close()


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property")
        result[key] = value
    return result


class GuestReviewService:
    def __init__(self, settings: ReviewSettings | None = None,
                 generate: Callable[[GuestReviewRequest, ReviewSettings], Awaitable[str]] = run_adk):
        self.settings = settings or ReviewSettings.from_environment()
        self.settings.validate()
        self.generate = generate

    async def analyze(self, context: GuestReviewRequest) -> GuestReviewResponse:
        from src.agents.guest_filtering_agent import PROMPT_VERSION
        decision = await generate_structured(context, self.settings, self.generate, GuestDecision)
        logger.info("Guest AI analysis completed: %s", decision.decision)
        return GuestReviewResponse(**decision.model_dump(), model=self.settings.model, prompt_version=PROMPT_VERSION)


async def generate_structured(context, settings, generate, schema):
    try:
        async with asyncio.timeout(settings.timeout_seconds):
            raw = await generate(context, settings)
        if not isinstance(raw, str) or len(raw.encode("utf-8")) > 16_384:
            raise ValueError("Invalid response size")
        return schema.model_validate(json.loads(raw, object_pairs_hook=unique_object))
    except TimeoutError:
        raise AnalysisError("ai_timeout", 504) from None
    except (ValueError, ValidationError):
        raise AnalysisError("invalid_ai_response", 502) from None
    except AnalysisError:
        raise
    except Exception:
        raise AnalysisError("ai_unavailable", 503) from None


def get_guest_review_service() -> GuestReviewService:
    return GuestReviewService()
