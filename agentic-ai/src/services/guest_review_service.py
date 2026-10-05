import asyncio
import json
import logging
import os
from collections.abc import Awaitable, Callable
from contextlib import aclosing
from dataclasses import dataclass
from typing import TypeVar
from uuid import uuid4

import httpx
from pydantic import BaseModel, ValidationError

from src.agents.adk_schema import AdkSchemaError
from src.coordinator_agent.config import (
    AGENTIC_AI_REQUEST_TIMEOUT_SECONDS,
    get_settings,
)
from src.models.guest_review_models import GuestDecision, GuestReviewRequest, GuestReviewResponse

logger = logging.getLogger(__name__)
OutputT = TypeVar("OutputT", bound=BaseModel)


class AnalysisError(Exception):
    def __init__(self, code: str, status_code: int):
        super().__init__(code)
        self.code = code
        self.status_code = status_code


@dataclass(frozen=True)
class AdkGenerationResult:
    model: str
    text: str


@dataclass(frozen=True)
class ReviewSettings:
    model: str = "gemini-2.5-flash"
    api_keys: tuple[str, ...] = ()
    fallback_models: tuple[str, ...] = ()
    timeout_seconds: float = float(AGENTIC_AI_REQUEST_TIMEOUT_SECONDS)

    @classmethod
    def from_environment(cls) -> "ReviewSettings":
        try:
            app_settings = get_settings()
            configured_models = app_settings.get_gemini_models()
            settings = cls(
                model=configured_models[0],
                api_keys=app_settings.get_gemini_api_keys(),
                fallback_models=configured_models[1:],
                timeout_seconds=max(
                    float(
                        os.getenv(
                            "GUEST_AI_TIMEOUT_SECONDS",
                            str(AGENTIC_AI_REQUEST_TIMEOUT_SECONDS),
                        )
                    ),
                    AGENTIC_AI_REQUEST_TIMEOUT_SECONDS,
                ),
            )
            settings.validate()
            return settings
        except (ValueError, TypeError):
            raise AnalysisError("ai_unavailable", 503) from None

    @property
    def models(self) -> tuple[str, ...]:
        return tuple(dict.fromkeys((self.model, *self.fallback_models)))

    def validate(self) -> None:
        if (
            not 0 < self.timeout_seconds <= AGENTIC_AI_REQUEST_TIMEOUT_SECONDS
            or any(
                not model.startswith("gemini-") or any(char.isspace() for char in model)
                for model in self.models
            )
        ):
            raise ValueError("Invalid Gemini review configuration")


async def run_adk(
    context, settings: ReviewSettings, agent_factory=None
) -> AdkGenerationResult:
    from google.adk.agents.run_config import RunConfig
    from google.adk.models import Gemini
    from google.adk.runners import Runner
    from google.adk.sessions import InMemorySessionService
    from google.genai import Client, errors, types
    from src.agents.guest_filtering_agent import create_agent

    agent_factory = agent_factory or create_agent

    if not settings.api_keys:
        raise AnalysisError("ai_unavailable", 503)

    invalid_key_indices: set[int] = set()
    invalid_models: set[str] = set()
    transient_failures: list[Exception] = []
    for model_name in settings.models:
        if model_name in invalid_models:
            continue
        for key_index, api_key in enumerate(settings.api_keys):
            if key_index in invalid_key_indices:
                continue
            client = Client(
                api_key=api_key,
                http_options=types.HttpOptions(
                    timeout=int(settings.timeout_seconds * 1000),
                    retry_options=types.HttpRetryOptions(attempts=1),
                ),
            )
            try:
                model = Gemini(model=model_name, client=client)
                sessions = InMemorySessionService()
                session_id = uuid4().hex
                agent = agent_factory(model)
                runner = Runner(app_name=agent.name, agent=agent, session_service=sessions)
                session_created = False
                try:
                    await sessions.create_session(
                        app_name=agent.name,
                        user_id="analysis",
                        session_id=session_id,
                    )
                    session_created = True
                    async with aclosing(
                        runner.run_async(
                            user_id="analysis",
                            session_id=session_id,
                            new_message=types.Content(
                                role="user",
                                parts=[
                                    types.Part(
                                        text=context.model_dump_json(by_alias=True)
                                    )
                                ],
                            ),
                            run_config=RunConfig(max_llm_calls=1),
                        )
                    ) as events:
                        async for event in events:
                            if event.is_final_response() and event.content:
                                return AdkGenerationResult(
                                    model=model_name,
                                    text="".join(
                                        part.text
                                        for part in event.content.parts or []
                                        if part.text and not part.thought
                                    ),
                                )
                    raise AnalysisError("invalid_ai_response", 502)
                finally:
                    try:
                        if session_created:
                            await sessions.delete_session(
                                app_name=agent.name,
                                user_id="analysis",
                                session_id=session_id,
                            )
                    finally:
                        try:
                            await runner.close()
                        finally:
                            await client.aio.aclose()
            except errors.APIError as exc:
                status_code = int(exc.code or 0)
                if status_code in {401, 403}:
                    invalid_key_indices.add(key_index)
                    logger.warning(
                        "Guest AI Gemini credentials rejected model=%s key_index=%d",
                        model_name,
                        key_index + 1,
                    )
                    continue
                if status_code == 404:
                    invalid_models.add(model_name)
                    logger.warning("Guest AI Gemini model unavailable model=%s", model_name)
                    break
                if status_code == 429 or status_code >= 500:
                    transient_failures.append(exc)
                    logger.warning(
                        "Guest AI Gemini provider failure model=%s status=%d",
                        model_name,
                        status_code,
                    )
                    continue
                if status_code == 400:
                    logger.error(
                        "Guest AI Gemini rejected request model=%s status=%d error=%s",
                        model_name,
                        status_code,
                        type(exc).__name__,
                    )
                    raise AnalysisError("ai_request_invalid", 502) from None
                raise AnalysisError("ai_unavailable", 503) from None
            except AdkSchemaError as exc:
                logger.error("Guest AI response schema rejected: %s", exc)
                raise AnalysisError("invalid_output_schema", 500) from None
            except httpx.TimeoutException as exc:
                transient_failures.append(exc)
                logger.warning(
                    "Guest AI Gemini request timed out model=%s", model_name
                )
            except (httpx.TransportError, ConnectionError, TimeoutError) as exc:
                transient_failures.append(exc)
                logger.warning(
                    "Guest AI Gemini transport failure model=%s error=%s",
                    model_name,
                    type(exc).__name__,
                )

    if transient_failures:
        if isinstance(transient_failures[-1], (httpx.TimeoutException, TimeoutError)):
            raise AnalysisError("ai_timeout", 504) from None
        raise AnalysisError("ai_unavailable", 503) from None
    raise AnalysisError("ai_unavailable", 503)


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property")
        result[key] = value
    return result


class GuestReviewService:
    def __init__(self, settings: ReviewSettings | None = None,
                 generate: Callable[
                     [GuestReviewRequest, ReviewSettings],
                     Awaitable[str | AdkGenerationResult],
                 ] = run_adk):
        self.settings = settings or ReviewSettings.from_environment()
        self.settings.validate()
        self.generate = generate

    async def analyze(self, context: GuestReviewRequest) -> GuestReviewResponse:
        from src.agents.guest_filtering_agent import PROMPT_VERSION

        decision, model_name = await generate_structured_with_model(
            context, self.settings, self.generate, GuestDecision
        )
        logger.info("Guest AI analysis completed: %s", decision.decision)
        return GuestReviewResponse(
            **decision.model_dump(),
            model=model_name,
            prompt_version=PROMPT_VERSION,
        )


async def generate_structured(context, settings, generate, schema: type[OutputT]) -> OutputT:
    result, _ = await generate_structured_with_model(
        context, settings, generate, schema
    )
    return result


async def generate_structured_with_model(
    context,
    settings: ReviewSettings,
    generate: Callable[
        [GuestReviewRequest, ReviewSettings],
        Awaitable[str | AdkGenerationResult],
    ],
    schema: type[OutputT],
) -> tuple[OutputT, str]:
    try:
        async with asyncio.timeout(settings.timeout_seconds):
            generated = await generate(context, settings)
    except TimeoutError:
        raise AnalysisError("ai_timeout", 504) from None
    except AnalysisError:
        raise
    except httpx.TimeoutException:
        raise AnalysisError("ai_timeout", 504) from None
    except (httpx.TransportError, ConnectionError):
        raise AnalysisError("ai_unavailable", 503) from None
    if isinstance(generated, AdkGenerationResult):
        raw = generated.text
        model_name = generated.model
    else:
        raw = generated
        model_name = settings.model
    if not isinstance(raw, str) or len(raw.encode("utf-8")) > 16_384:
        raise AnalysisError("invalid_ai_response", 502) from None
    try:
        result = schema.model_validate(
            json.loads(raw, object_pairs_hook=unique_object)
        )
    except (ValueError, ValidationError):
        raise AnalysisError("invalid_ai_response", 502) from None
    return result, model_name


def get_guest_review_service() -> GuestReviewService:
    return GuestReviewService()
