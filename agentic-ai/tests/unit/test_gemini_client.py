import asyncio
import json
import logging
from typing import Any

import pytest
from google.api_core.exceptions import (
    DeadlineExceeded,
    NotFound,
    ResourceExhausted,
    ServiceUnavailable,
    Unauthenticated,
)
from google.generativeai import protos
from pydantic import ValidationError

from src.coordinator_agent.config import Settings
from src.coordinator_agent.models import CoordinatorPlanOutput
from src.coordinator_agent.nodes.analyze import AnalysisOutput
from src.coordinator_agent.nodes.assess import BudgetOutput
from src.gemini_client import client as gemini_client_module
from src.gemini_client.client import GeminiClient, configure_gemini
from src.gemini_client.exceptions import (
    GeminiConfigurationError,
    GeminiInvalidCredentialsError,
    GeminiInvalidModelError,
    GeminiNetworkError,
    GeminiQuotaError,
    GeminiRateLimitError,
    GeminiResponseError,
    GeminiTimeoutError,
    GeminiTokenLimitError,
)
from src.gemini_client.schema import pydantic_to_gemini_schema


def _plan_payload() -> dict[str, Any]:
    return {
        "service_categories": ["Catering"],
        "budget_allocation": [],
        "target_vendor_types": ["Caterer"],
        "proposed_timeline": [
            {"phase_name": "A", "timing": "Now", "description": "Do A"},
            {"phase_name": "B", "timing": "Later", "description": "Do B"},
            {"phase_name": "C", "timing": "Event", "description": "Do C"},
        ],
        "rationale": "A suitable plan",
        "identified_risks": [],
        "missing_requirements": [],
        "plan_completeness_score": 90,
        "validation_summary": "Valid",
    }


class _Response:
    def __init__(self, text: str | None = None) -> None:
        self.text = text or json.dumps(_plan_payload())


class _Model:
    def __init__(self) -> None:
        self.generation_config: dict[str, Any] | None = None
        self.call_count = 0

    async def generate_content_async(self, *_args: Any, **kwargs: Any) -> _Response:
        self.call_count += 1
        self.generation_config = kwargs["generation_config"]
        schema = self.generation_config["response_schema"]
        if "analysis" in schema["properties"]:
            return _Response(json.dumps({"analysis": "A detailed event analysis."}))
        return _Response()


class _NotFoundModel(_Model):
    def __init__(self) -> None:
        super().__init__()
        self.call_count = 0

    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        self.call_count += 1
        raise NotFound("model not found")


class _DeadlineModel(_Model):
    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        raise DeadlineExceeded("request timed out")


class _QuotaModel(_Model):
    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        raise ResourceExhausted("project quota exhausted")


class _CredentialsModel(_Model):
    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        raise Unauthenticated("invalid API key")


class _RateLimitOnceModel(_Model):
    def __init__(self) -> None:
        super().__init__()
        self.did_limit = False

    async def generate_content_async(self, *args: Any, **kwargs: Any) -> _Response:
        if not self.did_limit:
            self.did_limit = True
            raise ResourceExhausted("per-minute rate limit")
        return await super().generate_content_async(*args, **kwargs)


class _RateLimitModel(_Model):
    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        raise ResourceExhausted("too many requests")


class _RetryModel(_Model):
    def __init__(self, failures: int) -> None:
        super().__init__()
        self.failures = failures
        self.attempt_count = 0

    async def generate_content_async(self, *_args: Any, **kwargs: Any) -> _Response:
        self.attempt_count += 1
        if self.attempt_count <= self.failures:
            raise ServiceUnavailable("temporarily unavailable")
        return await super().generate_content_async(**kwargs)


def test_client_generates_validated_plan_without_api_key() -> None:
    model = _Model()
    client = GeminiClient(model=model)
    plan = asyncio.run(client.generate_plan({"name": "Gala"}, CoordinatorPlanOutput))
    assert isinstance(plan, CoordinatorPlanOutput)
    assert client.count_tokens("12345678") == 2


def test_client_rejects_token_limit() -> None:
    client = GeminiClient(model=_Model(), max_input_tokens=1)
    with pytest.raises(GeminiTokenLimitError):
        asyncio.run(client.generate_plan({"name": "Gala"}, CoordinatorPlanOutput))


def test_client_rejects_invalid_response() -> None:
    client = GeminiClient(model=_Model())
    with pytest.raises(GeminiResponseError):
        client.validate_response({"plan_completeness_score": 101}, CoordinatorPlanOutput)


def test_client_passes_legacy_sdk_compatible_schema() -> None:
    model = _Model()
    client = GeminiClient(model=model)

    asyncio.run(client.generate_with_prompt("Analyze this event.", AnalysisOutput))

    assert model.generation_config is not None
    response_schema = model.generation_config["response_schema"]
    assert "title" not in response_schema
    assert "maxLength" not in response_schema["properties"]["analysis"]
    assert "minLength" not in response_schema["properties"]["analysis"]
    protos.Schema(response_schema)


def test_successful_response_is_cached(monkeypatch: pytest.MonkeyPatch) -> None:
    model = _Model()
    monkeypatch.setattr(
        gemini_client_module,
        "_configured_model",
        lambda *_args: model,
    )
    monkeypatch.setattr(
        gemini_client_module,
        "get_settings",
        lambda: Settings(
            _env_file=None,
            gemini_api_key="cache-test-key",
            gemini_model="gemini-cache-test",
        ),
    )
    prompt = "Use the successful response cache for this unique request."

    try:
        first = asyncio.run(
            GeminiClient().generate_with_prompt(prompt, AnalysisOutput)
        )
        second = asyncio.run(
            GeminiClient().generate_with_prompt(prompt, AnalysisOutput)
        )
    finally:
        monkeypatch.undo()

    assert first == second
    assert model.call_count == 1


def test_nested_coordinator_schema_is_compatible_with_legacy_sdk() -> None:
    response_schema = pydantic_to_gemini_schema(CoordinatorPlanOutput)

    assert "$defs" not in response_schema
    assert response_schema["properties"]["budget_allocation"]["items"]["properties"][
        "category"
    ]["type_"] == "STRING"
    protos.Schema(response_schema)


def test_budget_output_uses_representable_array_schema() -> None:
    response_schema = pydantic_to_gemini_schema(BudgetOutput)

    allocation = response_schema["properties"]["allocation"]
    assert allocation["type_"] == "ARRAY"
    assert allocation["items"]["type_"] == "OBJECT"
    assert "additionalProperties" not in allocation["items"]
    protos.Schema(response_schema)


def test_model_not_found_is_not_retried() -> None:
    model = _NotFoundModel()
    client = GeminiClient(model=model, max_retries=3)

    with pytest.raises(GeminiInvalidModelError):
        asyncio.run(
            client.generate_with_prompt(
                "Do not retry this missing-model case.", AnalysisOutput
            )
        )

    assert model.call_count == 1


def test_gemini_deadline_is_reported_as_timeout() -> None:
    client = GeminiClient(model=_DeadlineModel(), max_retries=0)

    with pytest.raises(GeminiTimeoutError):
        asyncio.run(
            client.generate_with_prompt(
                "Report a timeout for this unique request.", AnalysisOutput
            )
        )


def test_invalid_model_identifier_fails_settings_validation() -> None:
    with pytest.raises(ValidationError):
        Settings(_env_file=None, gemini_model="not-a-gemini-model")


def test_gemini_model_is_loaded_from_environment(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setenv("GEMINI_MODEL", "gemini-2.5-flash")
    monkeypatch.setenv("GEMINI_API_KEYS", "second-key, third-key")
    monkeypatch.setenv("GEMINI_FALLBACK_MODELS", "gemini-2.0-flash")

    settings = Settings(_env_file=None)

    assert settings.gemini_model == "gemini-2.5-flash"
    assert settings.get_gemini_api_keys() == ("second-key", "third-key")
    assert settings.get_gemini_models() == ("gemini-2.5-flash", "gemini-2.0-flash")


def test_quota_rotates_to_secondary_api_key_without_logging_secrets(
    monkeypatch: pytest.MonkeyPatch,
    caplog: pytest.LogCaptureFixture,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_api_keys="secondary-test-key",
        gemini_model="gemini-primary",
        gemini_max_retries=0,
    )
    monkeypatch.setattr(
        gemini_client_module, "get_settings", lambda: settings
    )
    models: list[tuple[str, str]] = []

    def create_model(model_name: str, api_key: str) -> _Model:
        models.append((model_name, api_key))
        return _QuotaModel() if api_key == "primary-test-key" else _Model()

    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)
    caplog.set_level(logging.INFO)

    result = asyncio.run(
        GeminiClient().generate_with_prompt(
            "Rotate to another key for this unique request.",
            AnalysisOutput,
            node_name="test_key_rotation",
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert models == [
        ("gemini-primary", "primary-test-key"),
        ("gemini-primary", "secondary-test-key"),
    ]
    assert "primary-test-key" not in caplog.text
    assert "secondary-test-key" not in caplog.text
    assert "****-key" in caplog.text
    assert "api_key_index=2" in caplog.text


def test_quota_falls_back_to_next_model(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_model="gemini-primary",
        gemini_fallback_models="gemini-fallback",
        gemini_max_retries=0,
    )
    monkeypatch.setattr(
        gemini_client_module, "get_settings", lambda: settings
    )
    models: list[str] = []

    def create_model(model_name: str, _api_key: str) -> _Model:
        models.append(model_name)
        return _QuotaModel() if model_name == "gemini-primary" else _Model()

    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    result = asyncio.run(
        GeminiClient().generate_with_prompt(
            "Use model fallback for this unique request.",
            AnalysisOutput,
            node_name="test_model_fallback",
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert models == ["gemini-primary", "gemini-fallback"]


def test_unavailable_model_is_skipped_for_remaining_keys(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_api_keys="secondary-test-key",
        gemini_model="gemini-unavailable",
        gemini_fallback_models="gemini-fallback",
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    models: list[tuple[str, str]] = []

    def create_model(model_name: str, api_key: str) -> _Model:
        models.append((model_name, api_key))
        return _NotFoundModel() if model_name == "gemini-unavailable" else _Model()

    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    result = asyncio.run(
        GeminiClient().generate_with_prompt(
            "Skip an unavailable model without trying every key.",
            AnalysisOutput,
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert models == [
        ("gemini-unavailable", "primary-test-key"),
        ("gemini-fallback", "primary-test-key"),
    ]


def test_transient_errors_use_exponential_backoff(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_model="gemini-primary",
        gemini_max_retries=5,
    )
    model = _RetryModel(failures=2)
    delays: list[float] = []

    async def record_delay(delay: float) -> None:
        delays.append(delay)

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", lambda *_args: model)
    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", record_delay)

    result = asyncio.run(
        GeminiClient(retry_delay=1.0).generate_with_prompt(
            "Retry with exponential backoff for this unique request.",
            AnalysisOutput,
            node_name="test_backoff",
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert model.call_count == 1
    assert model.attempt_count == 3
    assert delays == [1.0, 2.0]


def test_rate_limits_retry_with_backoff(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_model="gemini-primary",
        gemini_max_retries=1,
    )
    model = _RateLimitOnceModel()
    delays: list[float] = []

    async def record_delay(delay: float) -> None:
        delays.append(delay)

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", record_delay)

    result = asyncio.run(
        GeminiClient(model=model, retry_delay=1.0).generate_with_prompt(
            "Retry this rate-limited request once.",
            AnalysisOutput,
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert delays == [1.0]


def test_exhausted_rate_limit_has_a_domain_error() -> None:
    client = GeminiClient(model=_RateLimitModel(), max_retries=0, retry_delay=1.0)

    with pytest.raises(GeminiRateLimitError):
        asyncio.run(
            client.generate_with_prompt(
                "Return a rate limit domain error for this unique request.",
                AnalysisOutput,
            )
        )


def test_all_quota_fallbacks_are_bounded(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_api_keys="secondary-test-key",
        gemini_model="gemini-primary",
        gemini_fallback_models="gemini-fallback",
        gemini_max_retries=5,
    )
    calls: list[tuple[str, str]] = []

    def create_model(model_name: str, api_key: str) -> _Model:
        calls.append((model_name, api_key))
        return _QuotaModel()

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    with pytest.raises(GeminiQuotaError):
        asyncio.run(
            GeminiClient().generate_with_prompt(
                "Exhaust every configured fallback exactly once.",
                AnalysisOutput,
            )
        )

    assert calls == [
        ("gemini-primary", "primary-test-key"),
        ("gemini-primary", "secondary-test-key"),
        ("gemini-fallback", "primary-test-key"),
        ("gemini-fallback", "secondary-test-key"),
    ]


def test_invalid_credentials_rotate_to_another_key(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_api_keys="secondary-test-key",
        gemini_model="gemini-primary",
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(
        gemini_client_module,
        "_configured_model",
        lambda _model, key: _CredentialsModel() if key == "primary-test-key" else _Model(),
    )

    result = asyncio.run(
        GeminiClient().generate_with_prompt(
            "Recover from an invalid primary credential.",
            AnalysisOutput,
        )
    )

    assert result.analysis == "A detailed event analysis."


def test_all_invalid_credentials_have_a_domain_error(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_model="gemini-primary",
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(
        gemini_client_module, "_configured_model", lambda *_args: _CredentialsModel()
    )

    with pytest.raises(GeminiInvalidCredentialsError):
        asyncio.run(
            GeminiClient().generate_with_prompt(
                "Return a credentials domain error for this request.",
                AnalysisOutput,
            )
        )


def test_all_transient_failures_have_a_network_domain_error(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_model="gemini-primary",
        gemini_max_retries=1,
    )
    model = _RetryModel(failures=3)

    async def skip_delay(_delay: float) -> None:
        return None

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", lambda *_args: model)
    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", skip_delay)

    with pytest.raises(GeminiNetworkError):
        asyncio.run(
            GeminiClient().generate_with_prompt(
                "Return a network domain error for this request.",
                AnalysisOutput,
            )
        )

    assert model.attempt_count == 2


def test_startup_validation_does_not_spend_provider_quota(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setattr(
        gemini_client_module,
        "get_settings",
        lambda: Settings(
            _env_file=None, gemini_api_key="test-key", gemini_model="gemini-2.5-flash"
        ),
    )
    monkeypatch.setattr(
        gemini_client_module.genai,
        "configure",
        lambda **_kwargs: pytest.fail("startup must not configure a global Gemini key"),
    )
    monkeypatch.setattr(
        gemini_client_module.genai,
        "list_models",
        lambda **_kwargs: pytest.fail("startup must not make a Gemini API request"),
    )

    configure_gemini()


def test_startup_validation_requires_at_least_one_api_key(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setattr(
        gemini_client_module,
        "get_settings",
        lambda: Settings(_env_file=None, gemini_model="gemini-2.5-flash"),
    )
    monkeypatch.delenv("GEMINI_API_KEY", raising=False)
    monkeypatch.delenv("GEMINI_API_KEYS", raising=False)

    with pytest.raises(GeminiConfigurationError):
        configure_gemini()
