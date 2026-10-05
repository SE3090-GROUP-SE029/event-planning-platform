"""Async Gemini client with key health failover, model fallback, and output caching."""

import asyncio
import hashlib
import json
import logging
import math
import random
import socket
import threading
import time
from collections import OrderedDict
from collections.abc import AsyncIterator
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from functools import lru_cache
from types import SimpleNamespace
from typing import Any, TypeVar

from google import generativeai as genai
from google import genai as genai_v2
from google.ai.generativelanguage_v1beta.services.model_service.client import (
    ModelServiceClient,
)
from google.ai.generativelanguage_v1beta.services.generative_service.async_client import (
    GenerativeServiceAsyncClient,
)
from google.api_core.client_options import ClientOptions
from google.api_core.exceptions import (
    DeadlineExceeded,
    GoogleAPIError,
    InternalServerError,
    InvalidArgument,
    NotFound,
    PermissionDenied,
    ResourceExhausted,
    ServiceUnavailable,
    Unauthenticated,
)
import httpx
from pydantic import BaseModel, ValidationError

from src.coordinator_agent.config import (
    AGENTIC_AI_REQUEST_TIMEOUT_SECONDS,
    get_settings,
)
from src.coordinator_agent.prompts import PLAN_GENERATION_PROMPT

from .exceptions import (
    GeminiClientError,
    GeminiConfigurationError,
    GeminiInvalidCredentialsError,
    GeminiInvalidRequestError,
    GeminiInvalidModelError,
    GeminiNetworkError,
    GeminiQuotaError,
    GeminiResponseError,
    GeminiRateLimitError,
    GeminiSchemaError,
    GeminiTimeoutError,
    GeminiTokenLimitError,
)
from .schema import (
    gemini_schema_to_json_schema,
    pydantic_to_gemini_schema,
)
from .structured_output import StructuredOutputValidator

logger = logging.getLogger(__name__)
ModelT = TypeVar("ModelT", bound=BaseModel)
_RETRYABLE_ERRORS = (
    ServiceUnavailable,
    DeadlineExceeded,
    InternalServerError,
    httpx.TransportError,
    ConnectionError,
    socket.gaierror,
    TimeoutError,
    OSError,
)
_RETIRED_GEMINI_MODELS = {
    "gemini-3.7-flash",
    "gemini-3.6-flash",
    "gemini-3.5-flash-lite",
    "gemini-2.5-flash",
    "gemini-2.5-flash-preview",
    "gemini-2.5-flash-preview-05-20",
    "gemini-1.5-flash",
}
_RESPONSE_CACHE_TTL_SECONDS = 900
_RESPONSE_CACHE_MAX_ENTRIES = 512
_GEMINI_KEY_COOLDOWN_SECONDS = 10 * 60
_COORDINATOR_GEMINI_NODES = frozenset(
    {
        "analyze_requirements",
        "identify_service_categories",
        "propose_timeline",
        "allocate_budget",
        "assess_risks",
        "detect_missing_requirements",
        "generate_rationale",
    }
)
_response_cache: OrderedDict[str, tuple[float, str]] = OrderedDict()
_response_cache_lock = threading.Lock()
_gemini_key_pools: dict[tuple[str, ...], "_GeminiKeyPool"] = {}
_gemini_key_pools_lock = threading.Lock()


@dataclass
class _GeminiKeyHealth:
    status: str = "Healthy"
    last_success: str | None = None
    last_failure: str | None = None
    failure_count: int = 0
    cooldown_expires_at: float | None = None
    cooldown_expiry: str | None = None
    last_failure_reason: str | None = None


class _GeminiKeyPool:
    """Process-wide, thread-safe round-robin state for one configured key set."""

    def __init__(self, api_keys: tuple[str, ...]) -> None:
        self._fingerprints = tuple(
            hashlib.sha256(key.encode("utf-8")).hexdigest() for key in api_keys
        )
        self._lock = threading.Lock()
        self._cursor = 0
        self._health = [_GeminiKeyHealth() for _ in api_keys]

    def ordered_available_indices(self) -> tuple[int, ...]:
        now = time.monotonic()
        with self._lock:
            self._expire_cooldowns(now)
            available = [
                index for index in range(len(self._fingerprints))
                if self._health[index].status == "Healthy"
            ]
            if not available:
                return ()

            start = next(
                (position for position, index in enumerate(available) if index >= self._cursor),
                0,
            )
            ordered = available[start:] + available[:start]
            self._cursor = (ordered[0] + 1) % len(self._fingerprints)
            return tuple(ordered)

    def is_available(self, index: int) -> bool:
        with self._lock:
            self._expire_cooldowns(time.monotonic())
            return self._health[index].status == "Healthy"

    def quarantine(self, index: int, reason: str) -> tuple[str, str]:
        failed_at, cooldown_until, _ = self.record_failure(
            index,
            reason=reason,
            status="QuotaExceeded",
        )
        return failed_at, cooldown_until or ""

    def record_failure(
        self,
        index: int,
        *,
        reason: str,
        status: str,
        cooldown_seconds: int | None = _GEMINI_KEY_COOLDOWN_SECONDS,
    ) -> tuple[str, str | None, int]:
        now_monotonic = time.monotonic()
        failed_at = datetime.now(timezone.utc)
        cooldown_until = (
            failed_at + timedelta(seconds=cooldown_seconds)
            if cooldown_seconds is not None
            else None
        )
        with self._lock:
            health = self._health[index]
            health.status = status
            health.last_failure = failed_at.isoformat()
            health.failure_count += 1
            health.last_failure_reason = reason
            health.cooldown_expires_at = (
                now_monotonic + cooldown_seconds
                if cooldown_seconds is not None
                else None
            )
            health.cooldown_expiry = (
                cooldown_until.isoformat() if cooldown_until is not None else None
            )
            return failed_at.isoformat(), health.cooldown_expiry, health.failure_count

    def record_success(self, index: int) -> None:
        succeeded_at = datetime.now(timezone.utc).isoformat()
        with self._lock:
            health = self._health[index]
            health.status = "Healthy"
            health.last_success = succeeded_at
            health.cooldown_expires_at = None
            health.cooldown_expiry = None

    def available_count(self) -> int:
        with self._lock:
            self._expire_cooldowns(time.monotonic())
            return sum(health.status == "Healthy" for health in self._health)

    def diagnostics(self) -> dict[str, int | str]:
        with self._lock:
            self._expire_cooldowns(time.monotonic())
            return {
                "loaded_keys": len(self._fingerprints),
                "available_keys": sum(
                    health.status == "Healthy" for health in self._health
                ),
                "quarantined_keys": sum(
                    health.status != "Healthy" for health in self._health
                ),
                "project_count": "unknown",
            }

    def health_snapshot(self) -> tuple[dict[str, int | str | None], ...]:
        with self._lock:
            self._expire_cooldowns(time.monotonic())
            return tuple(
                {
                    "api_key_index": index + 1,
                    "status": health.status,
                    "last_success": health.last_success,
                    "last_failure": health.last_failure,
                    "failure_count": health.failure_count,
                    "cooldown_expiry": health.cooldown_expiry,
                    "last_failure_reason": health.last_failure_reason,
                }
                for index, health in enumerate(self._health)
            )

    def _expire_cooldowns(self, now: float) -> None:
        for index, health in enumerate(self._health):
            if (
                health.status not in {"CoolingDown", "QuotaExceeded", "Unavailable"}
                or health.cooldown_expires_at is None
                or health.cooldown_expires_at > now
            ):
                continue
            reason = health.last_failure_reason or "unknown"
            health.status = "Healthy"
            health.cooldown_expires_at = None
            health.cooldown_expiry = None
            logger.info(
                "Gemini API key cooldown expired api_key_index=%d previous_failure=%s",
                index + 1,
                reason,
            )


def _get_gemini_key_pool(api_keys: tuple[str, ...]) -> _GeminiKeyPool:
    fingerprint = tuple(
        hashlib.sha256(key.encode("utf-8")).hexdigest() for key in api_keys
    )
    with _gemini_key_pools_lock:
        pool = _gemini_key_pools.get(fingerprint)
        if pool is None:
            pool = _GeminiKeyPool(api_keys)
            _gemini_key_pools[fingerprint] = pool
        return pool


def _reset_gemini_key_pools() -> None:
    """Reset process-local key state; intended for isolated unit tests."""

    with _gemini_key_pools_lock:
        _gemini_key_pools.clear()


def gemini_key_pool_diagnostics(
    api_keys: tuple[str, ...],
) -> dict[str, int | str]:
    """Return non-secret key-pool health; project ownership is not inferable."""

    return _get_gemini_key_pool(api_keys).diagnostics()


@lru_cache(maxsize=128)
def _configured_model(model_name: str, api_key: str) -> Any:
    """Build key-bound clients for the Interactions API and legacy streaming."""

    model: Any = genai.GenerativeModel(model_name)
    model._async_client = GenerativeServiceAsyncClient(
        client_options=ClientOptions(api_key=api_key)
    )
    return _GeminiInteractionsModel(
        model_name=model_name,
        interactions_client=genai_v2.Client(
            api_key=api_key,
            http_options=genai_v2.types.HttpOptions(
                retry_options=genai_v2.types.HttpRetryOptions(attempts=1)
            ),
        ),
        streaming_model=model,
    )


class _GeminiInteractionsModel:
    """Adapt Gemini structured requests to the current Interactions API."""

    def __init__(
        self,
        model_name: str,
        interactions_client: Any,
        streaming_model: Any,
    ) -> None:
        self.model_name = model_name
        self.interactions_client = interactions_client
        self.streaming_model = streaming_model

    async def generate_content_async(
        self,
        prompt: str,
        *,
        generation_config: dict[str, Any],
        stream: bool = False,
        request_options: dict[str, Any] | None = None,
    ) -> Any:
        if stream:
            return await self.streaming_model.generate_content_async(
                prompt,
                generation_config=generation_config,
                stream=True,
                request_options=request_options,
            )

        response_schema = gemini_schema_to_json_schema(
            generation_config["response_schema"]
        )
        interaction_generation_config = {
            name: generation_config[name]
            for name in ("temperature", "top_k", "top_p")
            if name in generation_config
        }
        request_options = request_options or {}
        try:
            interaction = await self.interactions_client.aio.interactions.create(
                model=self.model_name,
                input=prompt,
                generation_config=interaction_generation_config,
                response_format={
                    "type": "text",
                    "mime_type": generation_config["response_mime_type"],
                    "schema": response_schema,
                },
                store=False,
                timeout=(
                    round(request_options["timeout"] * 1000)
                    if request_options.get("timeout") is not None
                    else None
                ),
            )
        except genai_v2.errors.APIError as exc:
            message = getattr(exc, "message", None) or str(exc)
            status_code = getattr(exc, "code", None)
            error_metadata = json.dumps(
                getattr(exc, "response_json", None),
                ensure_ascii=False,
                default=str,
            ).casefold()
            quota_markers = (
                "resource_exhausted",
                "rate_limit_exceeded",
                "quota_exceeded",
                "quota exceeded",
                "rate limit exceeded",
            )
            if status_code in {403, 429} and any(
                marker in f"{message} {error_metadata}".casefold()
                for marker in quota_markers
            ):
                raise _translated_api_error(
                    ResourceExhausted,
                    message,
                    status_code,
                ) from exc
            if status_code == 400:
                raise _translated_api_error(
                    InvalidArgument,
                    message,
                    status_code,
                ) from exc
            if status_code == 401:
                raise _translated_api_error(
                    Unauthenticated,
                    message,
                    status_code,
                ) from exc
            if status_code == 403:
                raise _translated_api_error(
                    PermissionDenied,
                    message,
                    status_code,
                ) from exc
            if status_code == 404:
                raise _translated_api_error(
                    NotFound,
                    message,
                    status_code,
                ) from exc
            if status_code == 408:
                raise _translated_api_error(
                    DeadlineExceeded,
                    message,
                    status_code,
                ) from exc
            if status_code == 504:
                raise _translated_api_error(
                    DeadlineExceeded,
                    message,
                    status_code,
                ) from exc
            if status_code == 429:
                raise _translated_api_error(
                    ResourceExhausted,
                    message,
                    status_code,
                ) from exc
            if status_code == 500:
                raise _translated_api_error(
                    InternalServerError,
                    message,
                    status_code,
                ) from exc
            if isinstance(status_code, int) and status_code >= 500:
                raise _translated_api_error(
                    ServiceUnavailable,
                    message,
                    status_code,
                ) from exc
            raise _translated_api_error(
                GoogleAPIError,
                message,
                status_code if isinstance(status_code, int) else None,
            ) from exc

        return SimpleNamespace(text=getattr(interaction, "output_text", None))


@lru_cache(maxsize=32)
def _configured_model_service_client(api_key: str) -> ModelServiceClient:
    """Build a model-listing client scoped to one API key."""

    return ModelServiceClient(client_options=ClientOptions(api_key=api_key))


def _mask_api_key(api_key: str) -> str:
    return f"****{api_key[-4:]}" if len(api_key) > 4 else "****"


def _translated_api_error(
    error_type: type[GoogleAPIError],
    message: str,
    http_status_code: int | None,
) -> GoogleAPIError:
    translated = error_type(message)
    if http_status_code is not None:
        setattr(translated, "http_status_code", http_status_code)
    return translated


def _estimated_tokens(text: str) -> int:
    return max(1, len(text) // 4)


def _validated_timeout(value: float, name: str) -> float:
    if (
        isinstance(value, bool)
        or not isinstance(value, (int, float))
        or not math.isfinite(value)
        or value <= 0
    ):
        raise GeminiConfigurationError(
            f"{name} must be a finite positive number of seconds"
        )
    return float(value)


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


def _provider_status(exc: Exception) -> str:
    status_code = getattr(exc, "http_status_code", None)
    if status_code is None:
        status_code = getattr(exc, "code", None)
    if isinstance(status_code, int):
        return str(status_code)
    if isinstance(exc, (DeadlineExceeded, httpx.TimeoutException, TimeoutError)):
        return "timeout"
    return type(exc).__name__


class GeminiClient:
    """Wrap Gemini SDKs without logging secrets or user prompt contents."""

    def __init__(
        self,
        api_key: str | None = None,
        model_name: str | None = None,
        max_retries: int | None = None,
        retry_delay: float | None = None,
        timeout: float | None = None,
        operation_timeout: float | None = None,
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
        self.key_pool = (
            _get_gemini_key_pool(self.api_keys) if model is None else None
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
        default_provider_timeout = max(
            settings.gemini_timeout_seconds,
            AGENTIC_AI_REQUEST_TIMEOUT_SECONDS,
        )
        default_operation_timeout = max(
            settings.coordinator_timeout_seconds,
            AGENTIC_AI_REQUEST_TIMEOUT_SECONDS,
        )
        self.timeout = _validated_timeout(
            timeout if timeout is not None else default_provider_timeout,
            "timeout",
        )
        self.operation_timeout = _validated_timeout(
            operation_timeout
            if operation_timeout is not None
            else default_operation_timeout,
            "operation_timeout",
        )
        self.coordinator_operation_timeout = _validated_timeout(
            operation_timeout
            if operation_timeout is not None
            else default_operation_timeout,
            "coordinator_operation_timeout",
        )
        self.temperature = temperature
        self.top_k = top_k
        self.top_p = top_p
        self.max_input_tokens = max_input_tokens
        self.model = model
        self.validator = StructuredOutputValidator()
        logger.info(
            "Gemini client initialized provider_timeout_seconds=%.3f "
            "operation_timeout_seconds=%.3f "
            "coordinator_operation_timeout_seconds=%.3f",
            self.timeout,
            self.operation_timeout,
            self.coordinator_operation_timeout,
        )

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
        try:
            cache_key = _response_cache_key(prompt, schema, self.model_names)
        except ValueError as exc:
            logger.error(
                "Gemini response schema failed local validation node=%s schema=%s",
                node_name,
                schema.__name__,
            )
            raise GeminiSchemaError(
                "The response schema cannot be represented in Gemini structured output."
            ) from exc
        cached_json = None if self.model is not None else _get_cached_response(cache_key)
        if cached_json is not None:
            logger.info(
                "Gemini response cache hit node=%s prompt_tokens_estimate=%d",
                node_name,
                token_estimate,
            )
            return schema.model_validate_json(cached_json)

        operation_timeout = (
            self.coordinator_operation_timeout
            if node_name in _COORDINATOR_GEMINI_NODES
            else self.operation_timeout
        )
        response = await self._call_with_retry(
            prompt,
            schema,
            token_count=token_estimate,
            node_name=node_name,
            operation_timeout=operation_timeout,
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
            operation_timeout=self.operation_timeout,
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
        operation_timeout: float | None = None,
    ) -> Any:
        try:
            response_schema = pydantic_to_gemini_schema(schema)
        except ValueError as exc:
            logger.error(
                "Gemini response schema failed local validation node=%s schema=%s",
                node_name,
                schema.__name__,
            )
            raise GeminiSchemaError(
                "The response schema cannot be represented in Gemini structured output."
            ) from exc

        generation_config = {
            "temperature": self.temperature,
            "top_k": self.top_k,
            "top_p": self.top_p,
            "response_mime_type": "application/json",
            "response_schema": response_schema,
        }
        safe_payload: dict[str, Any]
        if stream:
            safe_payload = {
                "contents": [{"role": "user", "parts": [{"text": "<redacted>"}]}],
                "generation_config": generation_config,
            }
        else:
            safe_payload = {
                "input": "<redacted>",
                "generation_config": {
                    name: generation_config[name]
                    for name in ("temperature", "top_k", "top_p")
                    if name in generation_config
                },
                "response_format": {
                    "type": "text",
                    "mime_type": generation_config["response_mime_type"],
                    "schema": gemini_schema_to_json_schema(response_schema),
                },
                "store": False,
            }
        failures: list[tuple[str, Exception]] = []
        invalid_key_indices: set[int] = set()
        invalid_model_names: set[str] = set()
        key_order = (
            self.key_pool.ordered_available_indices()
            if self.key_pool is not None
            else (0,)
        )
        if not key_order:
            logger.error(
                "All %d Gemini API keys exhausted or cooling down node=%s",
                len(self.api_keys),
                node_name,
            )
            raise GeminiQuotaError(
                "All configured Gemini API keys are temporarily quarantined.",
                error_code="all_gemini_keys_exhausted",
                available_keys=0,
                retry_after=_GEMINI_KEY_COOLDOWN_SECONDS,
            )
        last_fallback_reason = "round_robin"
        operation_deadline = (
            asyncio.get_running_loop().time() + operation_timeout
            if operation_timeout is not None
            else None
        )

        for model_index, model_name in enumerate(self.model_names):
            if model_name in invalid_model_names:
                continue
            api_keys = ("",) if self.model is not None else self.api_keys
            for configured_key_index in key_order:
                if (
                    self.key_pool is not None
                    and not self.key_pool.is_available(configured_key_index)
                ):
                    continue
                api_key = api_keys[configured_key_index]
                key_index = configured_key_index + 1 if api_key else 0
                if key_index in invalid_key_indices:
                    continue
                if configured_key_index != key_order[0]:
                    logger.info(
                        "Gemini API key fallback node=%s model=%s "
                        "api_key_index=%d api_key=%s fallback_attempt=%d "
                        "fallback_reason=%s selected_project=unknown",
                        node_name,
                        model_name,
                        key_index,
                        _mask_api_key(api_key),
                        configured_key_index + 1,
                        last_fallback_reason,
                    )
                else:
                    logger.info(
                        "Gemini API key selected node=%s model=%s api_key_index=%d "
                        "key_pool_size=%d selection=round_robin selected_project=unknown",
                        node_name,
                        model_name,
                        key_index,
                        len(self.api_keys),
                    )
                if model_index > 0:
                    logger.info(
                        "Gemini model fallback node=%s model=%s fallback_attempt=%d",
                        node_name,
                        model_name,
                        model_index,
                    )
                if model_name.lower() in _RETIRED_GEMINI_MODELS:
                    invalid_model_names.add(model_name)
                    logger.error(
                        "Skipping retired Gemini model node=%s model=%s reason=retired_model",
                        node_name,
                        model_name,
                    )
                    continue
                if self.model is not None:
                    model = self.model
                else:
                    model = _configured_model(model_name, api_key)
                retry_count = 0
                while True:
                    remaining_timeout = (
                        operation_deadline - asyncio.get_running_loop().time()
                        if operation_deadline is not None
                        else self.timeout
                    )
                    if operation_deadline is not None and remaining_timeout <= 0:
                        cause = failures[-1][1] if failures else TimeoutError()
                        raise GeminiTimeoutError(
                            f"Gemini operation exceeded {operation_timeout:g} seconds"
                        ) from cause
                    attempt_budget = remaining_timeout
                    if operation_deadline is not None and self.key_pool is not None:
                        remaining_key_count = sum(
                            self.key_pool.is_available(index)
                            for index in key_order
                        )
                        if remaining_key_count > 1:
                            # Reserve time for rotation and response processing.
                            attempt_budget = (
                                remaining_timeout / remaining_key_count * 0.9
                            )
                    attempt_timeout = min(self.timeout, attempt_budget)
                    logger.info(
                        "Calling Gemini node=%s model=%s api_key_index=%d "
                        "api_key=%s retry_count=%d input_tokens_estimate=%d "
                        "attempt_timeout_seconds=%.3f selected_project=unknown",
                        node_name,
                        model_name,
                        key_index,
                        _mask_api_key(api_key) if api_key else "injected-model",
                        retry_count,
                        token_count,
                        attempt_timeout,
                    )
                    logger.info(
                        "Gemini request generation_config=%s "
                        "serialized_request_payload=%s",
                        json.dumps(safe_payload["generation_config"], sort_keys=True),
                        json.dumps(
                            {"model": model_name, **safe_payload}, sort_keys=True
                        ),
                    )
                    attempt_started_at = time.monotonic()
                    try:
                        response = await asyncio.wait_for(
                            model.generate_content_async(
                                prompt,
                                generation_config=generation_config,
                                stream=stream,
                                request_options={"timeout": attempt_timeout},
                            ),
                            timeout=attempt_timeout,
                        )
                        if stream:
                            async def collect_chunks() -> list[str]:
                                chunks: list[str] = []
                                async for chunk in response:
                                    text = getattr(chunk, "text", "")
                                    if text:
                                        chunks.append(text)
                                return chunks

                            if operation_deadline is None:
                                chunks = await collect_chunks()
                            else:
                                remaining_timeout = (
                                    operation_deadline
                                    - asyncio.get_running_loop().time()
                                )
                                chunks = await asyncio.wait_for(
                                    collect_chunks(),
                                    timeout=remaining_timeout,
                                )
                            if self.key_pool is not None:
                                self.key_pool.record_success(configured_key_index)
                                logger.info(
                                    "Gemini key %d succeeded node=%s model=%s",
                                    key_index,
                                    node_name,
                                    model_name,
                                )
                            logger.info(
                                "Gemini response complete node=%s model=%s "
                                "api_key_index=%d output_tokens_estimate=%d",
                                node_name,
                                model_name,
                                key_index,
                                _estimated_tokens("".join(chunks)),
                            )
                            return chunks
                        if self.key_pool is not None:
                            self.key_pool.record_success(configured_key_index)
                            logger.info(
                                "Gemini key %d succeeded node=%s model=%s",
                                key_index,
                                node_name,
                                model_name,
                            )
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
                        last_fallback_reason = category
                        health_status = (
                            "CoolingDown"
                            if _provider_status(exc) == "429"
                            else "QuotaExceeded"
                        )
                        self._record_failed_key(
                            configured_key_index,
                            health_status=health_status,
                            reason=category,
                            error=exc,
                            node_name=node_name,
                            model_name=model_name,
                            key_order=key_order,
                        )
                        break
                    except _RETRYABLE_ERRORS as exc:
                        provider_status = _provider_status(exc)
                        if provider_status in {"500", "502", "503", "504"} and retry_count < 1:
                            retry_count += 1
                            logger.warning(
                                "Retrying Gemini key %d once after HTTP %s "
                                "node=%s model=%s",
                                key_index,
                                provider_status,
                                node_name,
                                model_name,
                            )
                            continue
                        failures.append(("network", exc))
                        last_fallback_reason = "network"
                        health_status = (
                            "Unavailable"
                            if provider_status in {"500", "502", "503", "504"}
                            else "CoolingDown"
                        )
                        self._record_failed_key(
                            configured_key_index,
                            health_status=health_status,
                            reason="network",
                            error=exc,
                            node_name=node_name,
                            model_name=model_name,
                            key_order=key_order,
                        )
                        break
                    except (Unauthenticated, PermissionDenied) as exc:
                        failures.append(("credentials", exc))
                        invalid_key_indices.add(key_index)
                        last_fallback_reason = "credentials"
                        self._record_failed_key(
                            configured_key_index,
                            health_status="Failed",
                            reason="credentials",
                            error=exc,
                            node_name=node_name,
                            model_name=model_name,
                            key_order=key_order,
                        )
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
                    except InvalidArgument as exc:
                        logger.error(
                            "Gemini rejected structured request node=%s model=%s "
                            "api_key_index=%d http_status=%s provider_message=%s",
                            node_name,
                            model_name,
                            key_index,
                            getattr(exc, "code", None),
                            " ".join(str(exc).split())[:500],
                        )
                        raise GeminiInvalidRequestError(
                            "Gemini rejected the structured request arguments; "
                            "the response schema or generation config is incompatible"
                        ) from exc
                    except GoogleAPIError as exc:
                        provider_status = _provider_status(exc)
                        if provider_status in {"500", "502", "503", "504"} and retry_count < 1:
                            retry_count += 1
                            logger.warning(
                                "Retrying Gemini key %d once after HTTP %s "
                                "node=%s model=%s",
                                key_index,
                                provider_status,
                                node_name,
                                model_name,
                            )
                            continue
                        failures.append(("provider", exc))
                        last_fallback_reason = "provider"
                        health_status = (
                            "Unavailable"
                            if provider_status in {"500", "502", "503", "504"}
                            else "CoolingDown"
                        )
                        self._record_failed_key(
                            configured_key_index,
                            health_status=health_status,
                            reason="provider",
                            error=exc,
                            node_name=node_name,
                            model_name=model_name,
                            key_order=key_order,
                        )
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
                    finally:
                        logger.info(
                            "Gemini attempt finished node=%s model=%s "
                            "api_key_index=%d elapsed_seconds=%.3f",
                            node_name,
                            model_name,
                            key_index,
                            time.monotonic() - attempt_started_at,
                        )
                if model_name in invalid_model_names:
                    break

        if self.key_pool is not None and self.key_pool.available_count() == 0:
            logger.error(
                "All %d Gemini API keys exhausted node=%s failures=%d",
                len(self.api_keys),
                node_name,
                len(failures),
            )
        self._raise_provider_failure(
            failures,
            available_keys=(
                self.key_pool.available_count()
                if self.key_pool is not None
                else None
            ),
        )

    @staticmethod
    def _raise_provider_failure(
        failures: list[tuple[str, Exception]],
        available_keys: int | None = None,
    ) -> None:
        categories = {category for category, _ in failures}
        if available_keys == 0 and (
            not failures or categories <= {"quota", "rate_limit"}
        ):
            cause = failures[-1][1] if failures else None
            error = GeminiQuotaError(
                "All configured Gemini API keys are exhausted or temporarily quarantined.",
                error_code="all_gemini_keys_exhausted",
                available_keys=0,
                retry_after=_GEMINI_KEY_COOLDOWN_SECONDS,
            )
            if cause is not None:
                raise error from cause
            raise error
        if not failures:
            raise GeminiClientError("Gemini request failed without a provider response")
        cause = failures[-1][1]
        logger.error(
            "Gemini request failed across configured fallbacks categories=%s final_error=%s",
            ",".join(sorted(categories)),
            type(cause).__name__,
        )
        if categories <= {"quota", "rate_limit"}:
            if categories == {"rate_limit"}:
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
        if categories == {"network"}:
            if isinstance(
                cause,
                (DeadlineExceeded, httpx.TimeoutException, TimeoutError),
            ):
                raise GeminiTimeoutError(
                    "Gemini request timed out across all available API keys"
                ) from cause
            raise GeminiNetworkError(
                "Gemini is temporarily unavailable across all available API keys"
            ) from cause
        raise GeminiClientError(
            "Gemini failed across configured fallbacks; see logs for failure categories"
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

    def _record_failed_key(
        self,
        api_key_index: int,
        *,
        health_status: str,
        reason: str,
        error: Exception,
        node_name: str,
        model_name: str,
        key_order: tuple[int, ...],
    ) -> None:
        if self.key_pool is None:
            return
        cooldown_seconds = (
            None if health_status == "Failed" else _GEMINI_KEY_COOLDOWN_SECONDS
        )
        failed_at, cooldown_until, failure_count = self.key_pool.record_failure(
            api_key_index,
            reason=reason,
            status=health_status,
            cooldown_seconds=cooldown_seconds,
        )
        logger.warning(
            "Gemini key %d failed with %s node=%s model=%s "
            "health_status=%s failure_count=%d failure_time=%s cooldown_expiry=%s",
            api_key_index + 1,
            _provider_status(error),
            node_name,
            model_name,
            health_status,
            failure_count,
            failed_at,
            cooldown_until,
        )
        next_key_index = next(
            (
                index
                for index in key_order
                if index != api_key_index and self.key_pool.is_available(index)
            ),
            None,
        )
        if next_key_index is not None:
            logger.info(
                "Rotating to Gemini key %d node=%s model=%s reason=%s",
                next_key_index + 1,
                node_name,
                model_name,
                reason,
            )


def list_available_gemini_models(api_key: str | None = None) -> tuple[str, ...]:
    """List Gemini models visible to one configured API key for diagnostics."""

    settings = get_settings()
    api_keys = (api_key,) if api_key else settings.get_gemini_api_keys()
    api_keys = tuple(key for key in api_keys if key)
    if not api_keys:
        raise GeminiConfigurationError("GEMINI_API_KEY or GEMINI_API_KEYS is required")

    selected_key = api_keys[0]
    try:
        models = genai.list_models(
            client=_configured_model_service_client(selected_key),
            request_options={
                "timeout": max(
                    settings.gemini_timeout_seconds,
                    AGENTIC_AI_REQUEST_TIMEOUT_SECONDS,
                )
            },
        )
        names = []
        for model in models:
            name = str(getattr(model, "name", "") or "")
            if name.startswith("models/"):
                name = name.split("/", 1)[1]
            if name:
                names.append(name)
        return tuple(dict.fromkeys(names))
    except (Unauthenticated, PermissionDenied) as exc:
        logger.error(
            "Gemini model listing credentials rejected api_key=%s",
            _mask_api_key(selected_key),
        )
        raise GeminiInvalidCredentialsError(
            "Gemini rejected the configured API credentials"
        ) from exc
    except _RETRYABLE_ERRORS as exc:
        logger.warning("Gemini model listing failed with transient error=%s", type(exc).__name__)
        raise GeminiNetworkError(
            "Gemini model listing is temporarily unavailable"
        ) from exc
    except GoogleAPIError as exc:
        logger.error("Gemini model listing failed error=%s", type(exc).__name__)
        raise GeminiClientError("Gemini model listing failed") from exc


def configure_gemini() -> None:
    """Validate local Gemini configuration without spending quota at startup."""

    settings = get_settings()
    _validated_timeout(
        max(
            settings.gemini_timeout_seconds,
            AGENTIC_AI_REQUEST_TIMEOUT_SECONDS,
        ),
        "GEMINI_TIMEOUT_SECONDS",
    )
    _validated_timeout(
        max(
            settings.coordinator_timeout_seconds,
            AGENTIC_AI_REQUEST_TIMEOUT_SECONDS,
        ),
        "COORDINATOR_TIMEOUT_SECONDS",
    )
    if not settings.get_gemini_api_keys():
        raise GeminiConfigurationError("GEMINI_API_KEY or GEMINI_API_KEYS is required")
    settings.get_gemini_models()
