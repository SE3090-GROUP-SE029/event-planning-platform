"""Async Gemini client with bounded retry, model/key fallback, and output caching."""

import asyncio
import hashlib
import json
import logging
import threading
import time
from collections import OrderedDict
from collections.abc import AsyncIterator
from functools import lru_cache
from typing import Any, TypeVar

from google import generativeai as genai
from google.ai.generativelanguage_v1beta.services.generative_service.async_client import (
    GenerativeServiceAsyncClient,
)
from google.api_core.client_options import ClientOptions
from google.api_core.exceptions import (
    DeadlineExceeded,
    GoogleAPIError,
    InternalServerError,
    NotFound,
    PermissionDenied,
    ResourceExhausted,
    ServiceUnavailable,
    Unauthenticated,
)
from pydantic import BaseModel, ValidationError

from src.coordinator_agent.config import get_settings
from src.coordinator_agent.prompts import PLAN_GENERATION_PROMPT

from .exceptions import (
    GeminiClientError,
    GeminiConfigurationError,
    GeminiInvalidCredentialsError,
    GeminiInvalidModelError,
    GeminiNetworkError,
    GeminiQuotaError,
    GeminiResponseError,
    GeminiRateLimitError,
    GeminiTimeoutError,
    GeminiTokenLimitError,
)
from .schema import pydantic_to_gemini_schema
from .structured_output import StructuredOutputValidator

logger = logging.getLogger(__name__)
ModelT = TypeVar("ModelT", bound=BaseModel)
_RETRYABLE_ERRORS = (ServiceUnavailable, DeadlineExceeded, InternalServerError)
_RESPONSE_CACHE_TTL_SECONDS = 900
_RESPONSE_CACHE_MAX_ENTRIES = 512
_response_cache: OrderedDict[str, tuple[float, str]] = OrderedDict()
_response_cache_lock = threading.Lock()


@lru_cache(maxsize=128)
def _configured_model(model_name: str, api_key: str) -> Any:
    """Build a model with key-bound clients, avoiding the SDK's global key state."""

    model: Any = genai.GenerativeModel(model_name)
    model._async_client = GenerativeServiceAsyncClient(
        client_options=ClientOptions(api_key=api_key)
    )
    return model


def _mask_api_key(api_key: str) -> str:
    return f"****{api_key[-4:]}" if len(api_key) > 4 else "****"


def _estimated_tokens(text: str) -> int:
    return max(1, len(text) // 4)


def _response_cache_key(
    prompt: str, schema: type[BaseModel], model_names: tuple[str, ...]
) -> str:
    payload = json.dumps(
        {
            "prompt": prompt,
            "schema": pydantic_to_gemini_schema(schema),
            "models": model_names,
        },
        ensure_ascii=False,
        sort_keys=True,
    )
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def _get_cached_response(cache_key: str) -> str | None:
    now = time.monotonic()
    with _response_cache_lock:
        expired = [
            key for key, (expires_at, _) in _response_cache.items() if expires_at <= now
        ]
        for key in expired:
            del _response_cache[key]
        cached = _response_cache.get(cache_key)
        if cached is None:
            return None
        _response_cache.move_to_end(cache_key)
        return cached[1]


def _cache_response(cache_key: str, response_json: str) -> None:
    with _response_cache_lock:
        _response_cache[cache_key] = (
            time.monotonic() + _RESPONSE_CACHE_TTL_SECONDS,
            response_json,
        )
        _response_cache.move_to_end(cache_key)
        while len(_response_cache) > _RESPONSE_CACHE_MAX_ENTRIES:
            _response_cache.popitem(last=False)


def _is_rate_limit(exc: ResourceExhausted) -> bool:
    error_info = getattr(exc, "error_info", None)
    reason = str(getattr(error_info, "reason", "") or "").casefold()
    details = " ".join(str(item) for item in getattr(exc, "details", ())).casefold()
    message = f"{exc} {reason} {details}".casefold()
    return any(
        marker in message
        for marker in (
            "rate limit",
            "too many requests",
            "rate_limit",
            "per minute",
            "requests per",
        )
    )


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
        self.api_keys = (
            (api_key,)
            if api_key
            else settings.get_gemini_api_keys()
        )
        self.model_names = (
            (model_name or settings.gemini_model,)
            if model is not None
            else (model_name,) if model_name else settings.get_gemini_models()
        )
        if not self.api_keys and model is None:
            raise GeminiConfigurationError("At least one Gemini API key is required")
        self.max_retries = max_retries if max_retries is not None else settings.gemini_max_retries
        self.retry_delay = (
            retry_delay
            if retry_delay is not None
            else settings.gemini_retry_delay_seconds
        )
        self.retry_max_delay = (
            settings.gemini_retry_max_delay_seconds
            if retry_delay is None
            else max(settings.gemini_retry_max_delay_seconds, retry_delay)
        )
        self.timeout = timeout if timeout is not None else settings.gemini_timeout_seconds
        self.temperature = temperature
        self.top_k = top_k
        self.top_p = top_p
        self.max_input_tokens = max_input_tokens
        self.model = model
        self.validator = StructuredOutputValidator()

    async def __aenter__(self) -> "GeminiClient":
        return self

    async def __aexit__(self, *_: object) -> None:
        return None

    async def generate_plan(
        self, event_data: dict[str, Any], schema: type[ModelT]
    ) -> ModelT:
        """Generate and validate a non-streaming structured plan."""

        prompt = self._build_prompt(event_data, schema)
        return await self.generate_with_prompt(prompt, schema, node_name="generate_plan")

    async def generate_with_prompt(
        self,
        prompt: str,
        schema: type[ModelT],
        node_name: str = "unspecified",
    ) -> ModelT:
        """Generate a structured response from an already-rendered prompt."""

        token_estimate = self._check_token_limit(prompt)
        cache_key = _response_cache_key(prompt, schema, self.model_names)
        cached_json = None if self.model is not None else _get_cached_response(cache_key)
        if cached_json is not None:
            logger.info(
                "Gemini response cache hit node=%s prompt_tokens_estimate=%d",
                node_name,
                token_estimate,
            )
            return schema.model_validate_json(cached_json)

        response = await self._call_with_retry(
            prompt, schema, token_count=token_estimate, node_name=node_name
        )
        parsed = self._parse_response(response, schema)
        if self.model is None:
            _cache_response(cache_key, parsed.model_dump_json())
        return parsed

    async def generate_plan_with_streaming(
        self, event_data: dict[str, Any], schema: type[ModelT]
    ) -> AsyncIterator[str]:
        """Yield buffered response chunks so a retry never duplicates partial output."""

        prompt = self._build_prompt(event_data, schema)
        token_estimate = self._check_token_limit(prompt)
        chunks = await self._call_with_retry(
            prompt,
            schema,
            stream=True,
            token_count=token_estimate,
            node_name="generate_plan_streaming",
        )
        for chunk in chunks:
            yield chunk

    def count_tokens(self, prompt: str) -> int:
        """Return a local token estimate without spending a provider request."""

        return _estimated_tokens(prompt)

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
        node_name: str = "unspecified",
    ) -> Any:
        generation_config = {
            "temperature": self.temperature,
            "top_k": self.top_k,
            "top_p": self.top_p,
            "response_mime_type": "application/json",
            "response_schema": pydantic_to_gemini_schema(schema),
        }
        failures: list[tuple[str, Exception]] = []
        invalid_key_indices: set[int] = set()
        invalid_model_names: set[str] = set()
        retries_used = 0

        for model_index, model_name in enumerate(self.model_names):
            if model_name in invalid_model_names:
                continue
            api_keys = ("",) if self.model is not None else self.api_keys
            for configured_key_index, api_key in enumerate(api_keys):
                key_index = configured_key_index + 1 if api_key else 0
                if key_index in invalid_key_indices:
                    continue
                if configured_key_index > 0:
                    logger.info(
                        "Gemini API key fallback node=%s model=%s "
                        "api_key_index=%d api_key=%s fallback_attempt=%d",
                        node_name,
                        model_name,
                        key_index,
                        _mask_api_key(api_key),
                        configured_key_index,
                    )
                if model_index > 0:
                    logger.info(
                        "Gemini model fallback node=%s model=%s fallback_attempt=%d",
                        node_name,
                        model_name,
                        model_index,
                    )
                if self.model is not None:
                    model = self.model
                else:
                    model = _configured_model(model_name, api_key)
                retry_count = 0
                while True:
                    logger.info(
                        "Calling Gemini node=%s model=%s api_key_index=%d "
                        "api_key=%s retry_count=%d input_tokens_estimate=%d",
                        node_name,
                        model_name,
                        key_index,
                        _mask_api_key(api_key) if api_key else "injected-model",
                        retry_count,
                        token_count,
                    )
                    try:
                        response = await model.generate_content_async(
                            prompt,
                            generation_config=generation_config,
                            stream=stream,
                            request_options={"timeout": self.timeout},
                        )
                        if stream:
                            chunks: list[str] = []
                            async for chunk in response:
                                text = getattr(chunk, "text", "")
                                if text:
                                    chunks.append(text)
                            logger.info(
                                "Gemini response complete node=%s model=%s "
                                "api_key_index=%d output_tokens_estimate=%d",
                                node_name,
                                model_name,
                                key_index,
                                _estimated_tokens("".join(chunks)),
                            )
                            return chunks
                        logger.info(
                            "Gemini response complete node=%s model=%s "
                            "api_key_index=%d output_tokens_estimate=%d",
                            node_name,
                            model_name,
                            key_index,
                            _estimated_tokens(getattr(response, "text", "") or ""),
                        )
                        return response
                    except ResourceExhausted as exc:
                        category = "rate_limit" if _is_rate_limit(exc) else "quota"
                        failures.append((category, exc))
                        logger.warning(
                            "Gemini quota-related failure node=%s model=%s "
                            "api_key_index=%d api_key=%s retry_count=%d category=%s",
                            node_name,
                            model_name,
                            key_index,
                            _mask_api_key(api_key) if api_key else "injected-model",
                            retry_count,
                            category,
                        )
                        if category == "rate_limit" and retries_used < self.max_retries:
                            await self._wait_before_retry(
                                retries_used, node_name, model_name
                            )
                            retries_used += 1
                            retry_count += 1
                            continue
                        break
                    except _RETRYABLE_ERRORS as exc:
                        failures.append(("network", exc))
                        if retries_used >= self.max_retries:
                            logger.warning(
                                "Gemini transient failure node=%s model=%s "
                                "api_key_index=%d api_key=%s retry_budget_used=%d error=%s",
                                node_name,
                                model_name,
                                key_index,
                                _mask_api_key(api_key) if api_key else "injected-model",
                                retries_used,
                                type(exc).__name__,
                            )
                            break
                        await self._wait_before_retry(retries_used, node_name, model_name)
                        retries_used += 1
                        retry_count += 1
                    except (Unauthenticated, PermissionDenied) as exc:
                        failures.append(("credentials", exc))
                        invalid_key_indices.add(key_index)
                        logger.error(
                            "Gemini credentials rejected node=%s model=%s "
                            "api_key_index=%d api_key=%s",
                            node_name,
                            model_name,
                            key_index,
                            _mask_api_key(api_key) if api_key else "injected-model",
                        )
                        break
                    except NotFound as exc:
                        failures.append(("model", exc))
                        invalid_model_names.add(model_name)
                        logger.warning(
                            "Gemini model unavailable node=%s model=%s "
                            "api_key_index=%d api_key=%s",
                            node_name,
                            model_name,
                            key_index,
                            _mask_api_key(api_key) if api_key else "injected-model",
                        )
                        break
                    except GoogleAPIError as exc:
                        failures.append(("provider", exc))
                        logger.error(
                            "Gemini request rejected node=%s model=%s "
                            "api_key_index=%d api_key=%s error=%s",
                            node_name,
                            model_name,
                            key_index,
                            _mask_api_key(api_key) if api_key else "injected-model",
                            type(exc).__name__,
                        )
                        break
                if model_name in invalid_model_names:
                    break

        self._raise_provider_failure(failures)

    async def _wait_before_retry(
        self, retry_count: int, node_name: str, model_name: str
    ) -> None:
        delay = min(self.retry_delay * (2**retry_count), self.retry_max_delay)
        logger.warning(
            "Gemini retry scheduled node=%s model=%s retry_count=%d delay_seconds=%.1f",
            node_name,
            model_name,
            retry_count + 1,
            delay,
        )
        await asyncio.sleep(delay)

    @staticmethod
    def _raise_provider_failure(failures: list[tuple[str, Exception]]) -> None:
        if not failures:
            raise GeminiClientError("Gemini request failed without a provider response")
        categories = {category for category, _ in failures}
        cause = failures[-1][1]
        if "quota" in categories or "rate_limit" in categories:
            if "rate_limit" in categories and "quota" not in categories:
                raise GeminiRateLimitError(
                    "Gemini rate limit persists across configured fallbacks"
                ) from cause
            raise GeminiQuotaError(
                "Gemini quota is exhausted across configured keys and models"
            ) from cause
        if categories == {"credentials"}:
            raise GeminiInvalidCredentialsError(
                "All configured Gemini API keys were rejected"
            ) from cause
        if categories == {"model"}:
            raise GeminiInvalidModelError(
                "No configured Gemini model is available for this request"
            ) from cause
        if "network" in categories:
            if isinstance(cause, DeadlineExceeded):
                raise GeminiTimeoutError("Gemini request timed out after retries") from cause
            raise GeminiNetworkError(
                "Gemini is temporarily unavailable after bounded retries"
            ) from cause
        if "credentials" in categories:
            raise GeminiInvalidCredentialsError(
                "Gemini rejected the configured API credentials"
            ) from cause
        if "model" in categories:
            raise GeminiInvalidModelError(
                "Gemini rejected the configured model"
            ) from cause
        raise GeminiClientError(
            "Gemini rejected the request; verify model access and request configuration"
        ) from cause

    def _parse_response(self, response: Any, schema: type[ModelT]) -> ModelT:
        text = getattr(response, "text", None)
        if not text:
            raise GeminiResponseError("Gemini returned an empty response")
        try:
            payload = json.loads(text)
        except json.JSONDecodeError as exc:
            raise GeminiResponseError("Gemini returned invalid JSON") from exc
        return self.validate_response(payload, schema)

    def _build_prompt(self, event_data: dict[str, Any], schema: type[ModelT]) -> str:
        return PLAN_GENERATION_PROMPT.format(
            event_data=json.dumps(event_data, ensure_ascii=False, default=str),
            schema=json.dumps(pydantic_to_gemini_schema(schema), ensure_ascii=False),
        )

    def _check_token_limit(self, prompt: str) -> int:
        token_count = _estimated_tokens(prompt)
        if token_count > self.max_input_tokens:
            raise GeminiTokenLimitError(
                f"prompt uses approximately {token_count} tokens; "
                f"maximum is {self.max_input_tokens}"
            )
        return token_count


def configure_gemini() -> None:
    """Validate local Gemini configuration without spending quota at startup."""

    settings = get_settings()
    if not settings.get_gemini_api_keys():
        raise GeminiConfigurationError("GEMINI_API_KEY or GEMINI_API_KEYS is required")
    settings.get_gemini_models()
