"""Async Gemini client with retries and Pydantic structured-output validation."""

import asyncio
import json
import logging
import random
from collections.abc import AsyncIterator
from typing import Any, TypeVar

from google import generativeai as genai
from google.api_core.exceptions import GoogleAPIError, ResourceExhausted
from pydantic import BaseModel, ValidationError

from src.coordinator_agent.config import get_settings
from src.coordinator_agent.prompts import PLAN_GENERATION_PROMPT

from .exceptions import (
    GeminiClientError,
    GeminiConfigurationError,
    GeminiResponseError,
    GeminiTokenLimitError,
)
from .structured_output import StructuredOutputValidator

logger = logging.getLogger(__name__)
ModelT = TypeVar("ModelT", bound=BaseModel)


class GeminiClient:
    """Wrap the Google Generative AI SDK without logging secrets or payloads."""

    def __init__(
        self,
        api_key: str | None = None,
        model_name: str = "gemini-1.5-pro",
        max_retries: int = 3,
        retry_delay: float = 1.0,
        timeout: float = 60.0,
        temperature: float = 0.7,
        top_k: int = 40,
        top_p: float = 0.95,
        max_input_tokens: int = 30_000,
        model: Any | None = None,
    ) -> None:
        key = api_key or get_settings().gemini_api_key
        if not key and model is None:
            raise GeminiConfigurationError("GEMINI_API_KEY is required")
        self.api_key = key
        self.model_name = model_name
        self.max_retries = max_retries
        self.retry_delay = retry_delay
        self.timeout = timeout
        self.temperature = temperature
        self.top_k = top_k
        self.top_p = top_p
        self.max_input_tokens = max_input_tokens
        self.model = model
        self.validator = StructuredOutputValidator()
        if self.model is None:
            genai.configure(api_key=key)
            self.model = genai.GenerativeModel(model_name)

    async def __aenter__(self) -> "GeminiClient":
        return self

    async def __aexit__(self, *_: object) -> None:
        return None

    async def generate_plan(
        self, event_data: dict[str, Any], schema: type[ModelT]
    ) -> ModelT:
        """Generate and validate a non-streaming structured plan."""

        prompt = self._build_prompt(event_data, schema)
        self._check_token_limit(prompt)
        response = await self._call_with_retry(prompt, schema)
        return self._parse_response(response, schema)

    async def generate_with_prompt(self, prompt: str, schema: type[ModelT]) -> ModelT:
        """Generate a structured response from an already-rendered prompt."""

        self._check_token_limit(prompt)
        response = await self._call_with_retry(prompt, schema)
        return self._parse_response(response, schema)

    async def generate_plan_with_streaming(
        self, event_data: dict[str, Any], schema: type[ModelT]
    ) -> AsyncIterator[str]:
        """Yield response text chunks for long-running operations."""

        prompt = self._build_prompt(event_data, schema)
        self._check_token_limit(prompt)
        response = await self._call_with_retry(prompt, schema, stream=True)
        async for chunk in response:
            text = getattr(chunk, "text", "")
            if text:
                yield text

    def count_tokens(self, prompt: str) -> int:
        """Return the SDK token count, with a conservative local fallback."""

        try:
            response = self._require_model().count_tokens(prompt)
            return int(response.total_tokens)
        except (GoogleAPIError, AttributeError, TypeError) as exc:
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
        self, prompt: str, schema: type[ModelT], stream: bool = False
    ) -> Any:
        generation_config = {
            "temperature": self.temperature,
            "top_k": self.top_k,
            "top_p": self.top_p,
            "response_mime_type": "application/json",
            "response_schema": schema.model_json_schema(),
        }
        for attempt in range(self.max_retries + 1):
            try:
                logger.info(
                    "Calling Gemini model=%s attempt=%d tokens=%d",
                    self.model_name,
                    attempt + 1,
                    self.count_tokens(prompt),
                )
                return await self._require_model().generate_content_async(
                    prompt,
                    generation_config=generation_config,
                    stream=stream,
                    request_options={"timeout": self.timeout},
                )
            except (ResourceExhausted, GoogleAPIError) as exc:
                if attempt >= self.max_retries:
                    raise GeminiClientError("Gemini API request failed after retries") from exc
                delay = self.retry_delay * (2**attempt) + random.uniform(0, self.retry_delay)
                logger.warning("Gemini request retrying in %.2fs: %s", delay, type(exc).__name__)
                await asyncio.sleep(delay)

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
            schema=json.dumps(schema.model_json_schema(), ensure_ascii=True),
        )

    def _check_token_limit(self, prompt: str) -> None:
        token_count = self.count_tokens(prompt)
        if token_count > self.max_input_tokens:
            raise GeminiTokenLimitError(
                f"prompt uses {token_count} tokens; maximum is {self.max_input_tokens}"
            )


def configure_gemini() -> None:
    """Validate that Gemini can be configured without exposing the API key."""

    GeminiClient()
