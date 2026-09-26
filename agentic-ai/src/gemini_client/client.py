"""Async Gemini client with retries and Pydantic structured-output validation."""

import asyncio
import json
import logging
import random
from collections.abc import AsyncIterator
from typing import Any, TypeVar

from google import generativeai as genai
from google.api_core.exceptions import (
    DeadlineExceeded,
    GoogleAPIError,
    InternalServerError,
    ResourceExhausted,
    ServiceUnavailable,
)
from pydantic import BaseModel, ValidationError

from src.coordinator_agent.config import get_settings
from src.coordinator_agent.prompts import PLAN_GENERATION_PROMPT

from .exceptions import (
    GeminiClientError,
    GeminiConfigurationError,
    GeminiResponseError,
    GeminiTokenLimitError,
    GeminiTimeoutError,
)
from .schema import pydantic_to_gemini_schema
from .structured_output import StructuredOutputValidator

logger = logging.getLogger(__name__)
ModelT = TypeVar("ModelT", bound=BaseModel)


class GeminiClient:
    """Wrap the Google Generative AI SDK without logging secrets or payloads."""

    def __init__(
        self,
        api_key: str | None = None,
        model_name: str | None = None,
        max_retries: int | None = None,
        retry_delay: float | None = None,
        timeout: float | None = None,
        temperature: float = 0.7,
        top_k: int = 40,
        top_p: float = 0.95,
        max_input_tokens: int = 30_000,
        model: Any | None = None,
    ) -> None:
        settings = get_settings()
        key = api_key if api_key is not None else settings.gemini_api_key
        if not key and model is None:
            raise GeminiConfigurationError("GEMINI_API_KEY is required")
        self.api_key = key
        self.model_name = model_name or settings.gemini_model
        self.max_retries = max_retries if max_retries is not None else settings.gemini_max_retries
        self.retry_delay = (
            retry_delay if retry_delay is not None else settings.gemini_retry_delay_seconds
        )
        self.timeout = timeout if timeout is not None else settings.gemini_timeout_seconds
        self.temperature = temperature
        self.top_k = top_k
        self.top_p = top_p
        self.max_input_tokens = max_input_tokens
        self.model = model
        self.validator = StructuredOutputValidator()
        if self.model is None:
            genai.configure(api_key=key)
            self.model = genai.GenerativeModel(self.model_name)

    async def __aenter__(self) -> "GeminiClient":
        return self

    async def __aexit__(self, *_: object) -> None:
        return None

    async def generate_plan(
        self, event_data: dict[str, Any], schema: type[ModelT]
    ) -> ModelT:
        """Generate and validate a non-streaming structured plan."""

        prompt = self._build_prompt(event_data, schema)
        token_count = await self._check_token_limit(prompt)
        response = await self._call_with_retry(prompt, schema, token_count=token_count)
        return self._parse_response(response, schema)

    async def generate_with_prompt(self, prompt: str, schema: type[ModelT]) -> ModelT:
        """Generate a structured response from an already-rendered prompt."""

        token_count = await self._check_token_limit(prompt)
        response = await self._call_with_retry(prompt, schema, token_count=token_count)
        return self._parse_response(response, schema)

    async def generate_plan_with_streaming(
        self, event_data: dict[str, Any], schema: type[ModelT]
    ) -> AsyncIterator[str]:
        """Yield response text chunks for long-running operations."""

        prompt = self._build_prompt(event_data, schema)
        token_count = await self._check_token_limit(prompt)
        response = await self._call_with_retry(
            prompt, schema, stream=True, token_count=token_count
        )
        async for chunk in response:
            text = getattr(chunk, "text", "")
            if text:
                yield text

    def count_tokens(self, prompt: str) -> int:
        """Return the SDK token count, with a conservative local fallback."""

        try:
            response = self._require_model().count_tokens(
                prompt,
                request_options={"timeout": self.timeout},
            )
            return int(response.total_tokens)
        except (GoogleAPIError, AttributeError, TypeError, TimeoutError) as exc:
            logger.warning("Gemini token counting failed; using estimate: %s", type(exc).__name__)
            return max(1, len(prompt) // 4)

    async def _count_tokens_for_request(self, prompt: str) -> int:
        """Count tokens without blocking the event loop, bounded by Gemini timeout."""

        try:
            async_counter = getattr(self._require_model(), "count_tokens_async", None)
            if async_counter is None:
                return await asyncio.to_thread(self.count_tokens, prompt)
            response = await async_counter(
                prompt,
                request_options={
                    "timeout": get_settings().gemini_token_count_timeout_seconds
                },
            )
            return int(response.total_tokens)
        except (GoogleAPIError, AttributeError, TypeError, TimeoutError) as exc:
            logger.warning("Gemini token counting failed; using estimate: %s", type(exc).__name__)
            return max(1, len(prompt) // 4)

    def validate_response(self, response: Any, schema: type[ModelT]) -> ModelT:
        """Validate a parsed response against its requested Pydantic schema."""

        if isinstance(response, BaseModel):
            response = response.model_dump()
        if not isinstance(response, dict):
            raise GeminiResponseError("Gemini response must decode to a JSON object")
        try:
            return schema.model_validate(response)
        except ValidationError as exc:
            raise GeminiResponseError(
                f"Gemini response failed schema validation: {exc}"
            ) from exc

    async def _call_with_retry(
        self,
        prompt: str,
        schema: type[ModelT],
        stream: bool = False,
        token_count: int = 0,
    ) -> Any:
        generation_config = {
            "temperature": self.temperature,
            "top_k": self.top_k,
            "top_p": self.top_p,
            "response_mime_type": "application/json",
            "response_schema": pydantic_to_gemini_schema(schema),
        }
        for attempt in range(self.max_retries + 1):
            try:
                logger.info(
                    "Calling Gemini model=%s attempt=%d tokens=%d",
                    self.model_name,
                    attempt + 1,
                    token_count,
                )
                return await self._require_model().generate_content_async(
                    prompt,
                    generation_config=generation_config,
                    stream=stream,
                    request_options={"timeout": self.timeout},
                )
            except (
                ResourceExhausted,
                ServiceUnavailable,
                DeadlineExceeded,
                InternalServerError,
            ) as exc:
                if attempt >= self.max_retries:
                    if isinstance(exc, DeadlineExceeded):
                        raise GeminiTimeoutError(
                            "Gemini API request timed out after retries"
                        ) from exc
                    raise GeminiClientError("Gemini API request failed after retries") from exc
                delay = self.retry_delay * (2**attempt) + random.uniform(0, self.retry_delay)
                logger.warning("Gemini request retrying in %.2fs: %s", delay, type(exc).__name__)
                await asyncio.sleep(delay)
            except GoogleAPIError as exc:
                raise GeminiClientError(
                    "Gemini rejected the request; verify model access and request configuration"
                ) from exc

    def _parse_response(self, response: Any, schema: type[ModelT]) -> ModelT:
        text = getattr(response, "text", None)
        if not text:
            raise GeminiResponseError("Gemini returned an empty response")
        try:
            payload = json.loads(text)
        except json.JSONDecodeError as exc:
            raise GeminiResponseError("Gemini returned invalid JSON") from exc
        return self.validate_response(payload, schema)

    def _require_model(self) -> Any:
        if self.model is None:
            raise GeminiConfigurationError("Gemini model is not configured")
        return self.model

    def _build_prompt(self, event_data: dict[str, Any], schema: type[ModelT]) -> str:
        return PLAN_GENERATION_PROMPT.format(
            event_data=json.dumps(event_data, ensure_ascii=True, default=str),
            schema=json.dumps(pydantic_to_gemini_schema(schema), ensure_ascii=True),
        )

    async def _check_token_limit(self, prompt: str) -> int:
        token_count = await self._count_tokens_for_request(prompt)
        if token_count > self.max_input_tokens:
            raise GeminiTokenLimitError(
                f"prompt uses {token_count} tokens; maximum is {self.max_input_tokens}"
            )
        return token_count


def configure_gemini() -> None:
    """Verify API credentials and model availability during service startup."""

    settings = get_settings()
    if not settings.gemini_api_key:
        raise GeminiConfigurationError("GEMINI_API_KEY is required to start the AI service")

    genai.configure(api_key=settings.gemini_api_key)
    try:
        models = genai.list_models(
            request_options={"timeout": settings.gemini_validation_timeout_seconds}
        )
        model = next(
            (
                item
                for item in models
                if item.name.removeprefix("models/") == settings.gemini_model
            ),
            None,
        )
    except GoogleAPIError as exc:
        raise GeminiConfigurationError(
            "Unable to validate Gemini credentials or model availability"
        ) from exc

    if model is None:
        raise GeminiConfigurationError(
            f"Configured Gemini model {settings.gemini_model!r} is unavailable to this API key"
        )
    if "generateContent" not in model.supported_generation_methods:
        raise GeminiConfigurationError(
            f"Configured Gemini model {settings.gemini_model!r} does not support generateContent"
        )
