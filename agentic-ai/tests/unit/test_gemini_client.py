import asyncio
import json
import logging
import socket
import time
from types import SimpleNamespace
from typing import Any

import pytest
import httpx
from google.api_core.exceptions import (
    DeadlineExceeded,
    InternalServerError,
    InvalidArgument,
    NotFound,
    ResourceExhausted,
    ServiceUnavailable,
    Unauthenticated,
)
from google.generativeai import protos
from pydantic import BaseModel, ValidationError

from src.coordinator_agent.config import Settings
from src.coordinator_agent.models import CoordinatorPlanOutput
from src.coordinator_agent.nodes.analyze import AnalysisOutput
from src.coordinator_agent.nodes import analyze as analyze_module
from src.coordinator_agent.state import CoordinatorState
from src.coordinator_agent.nodes.assess import BudgetOutput
from src.gemini_client import client as gemini_client_module
from src.gemini_client.client import (
    GeminiClient,
    configure_gemini,
    list_available_gemini_models,
)
from src.gemini_client.exceptions import (
    GeminiClientError,
    GeminiConfigurationError,
    GeminiInvalidCredentialsError,
    GeminiInvalidRequestError,
    GeminiInvalidModelError,
    GeminiNetworkError,
    GeminiQuotaError,
    GeminiRateLimitError,
    GeminiResponseError,
    GeminiSchemaError,
    GeminiTimeoutError,
    GeminiTokenLimitError,
)
from src.gemini_client.schema import pydantic_to_gemini_schema
from src.vendor_analysis.models import VendorRecommendationResponse


def _plan_payload() -> dict[str, Any]:
    return {
        "service_categories": ["Catering"],
        "budget_allocation": [
            {
                "category": "Catering",
                "amount": 100.0,
                "percentage_of_total": 100.0,
            }
        ],
        "target_vendor_types": ["Caterer"],
        "proposed_timeline": [
            {"phase_name": "A", "timing": "Now", "description": "Do A"},
            {"phase_name": "B", "timing": "Later", "description": "Do B"},
            {"phase_name": "C", "timing": "Event", "description": "Do C"},
        ],
        "rationale": (
            "This complete plan balances the event priorities, confirms vendors "
            "early, and reserves adequate time for final confirmations."
        ),
        "identified_risks": [
            {
                "risk": "A supplier may become unavailable.",
                "severity": "Medium",
                "recommendation": "Confirm bookings and retain a backup option.",
            }
        ],
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


class _InvalidArgumentModel(_Model):
    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        self.call_count += 1
        raise InvalidArgument("Invalid response schema: unsupported UUID format")


class _VendorResponseModel(_Model):
    async def generate_content_async(self, *_args: Any, **kwargs: Any) -> _Response:
        self.call_count += 1
        self.generation_config = kwargs["generation_config"]
        return _Response(
            json.dumps(
                {
                    "recommendations": [
                        {
                            "vendorId": "00000000-0000-0000-0000-000000000001",
                            "vendorServiceId": None,
                            "score": 88,
                            "reasons": ["Matches the requested event category."],
                        }
                    ]
                }
            )
        )


class _DeadlineModel(_Model):
    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        raise DeadlineExceeded("request timed out")


class _HangingModel(_Model):
    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        await asyncio.Event().wait()
        return _Response()


class _HangingStreamResponse:
    def __aiter__(self) -> "_HangingStreamResponse":
        return self

    async def __anext__(self) -> Any:
        await asyncio.Event().wait()


class _HangingStreamModel(_Model):
    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> Any:
        return _HangingStreamResponse()


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


class _TransportFailureModel(_Model):
    def __init__(self, failure: Exception) -> None:
        super().__init__()
        self.failure = failure
        self.attempt_count = 0

    async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> _Response:
        self.attempt_count += 1
        raise self.failure


class _ListedModel:
    def __init__(self, name: str) -> None:
        self.name = name


def test_client_generates_validated_plan_without_api_key() -> None:
    model = _Model()
    client = GeminiClient(model=model)
    plan = asyncio.run(client.generate_plan({"name": "Gala"}, CoordinatorPlanOutput))
    assert isinstance(plan, CoordinatorPlanOutput)
    assert client.count_tokens("12345678") == 2


def test_client_initializes_timeout_attributes_with_explicit_values(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(_env_file=None)
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)

    client = GeminiClient(
        model=_Model(),
        timeout=12,
        operation_timeout=25,
    )

    assert client.timeout == 12
    assert client.operation_timeout == 25
    assert client.coordinator_operation_timeout == 25


def test_client_defaults_to_30_minute_timeouts_when_configuration_is_missing(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key=None,
        gemini_api_keys="",
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)

    client = GeminiClient(model=_Model())

    assert client.timeout == 1800
    assert client.operation_timeout == 1800
    assert client.coordinator_operation_timeout == 1800


def test_client_does_not_allow_legacy_short_timeout_settings(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key=None,
        gemini_api_keys="",
        gemini_timeout_seconds=15,
        coordinator_timeout_seconds=240,
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)

    client = GeminiClient(model=_Model())

    assert client.timeout == 1800
    assert client.operation_timeout == 1800
    assert client.coordinator_operation_timeout == 1800


@pytest.mark.parametrize("timeout", [0, -1, float("inf"), float("nan")])
def test_client_rejects_invalid_operation_timeout(
    timeout: float,
) -> None:
    with pytest.raises(GeminiConfigurationError, match="operation_timeout"):
        GeminiClient(model=_Model(), operation_timeout=timeout)


def test_requirements_analysis_node_executes_with_initialized_client_timeouts(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(_env_file=None, gemini_api_key=None, gemini_api_keys="")
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    clients: list[GeminiClient] = []

    def create_client() -> GeminiClient:
        client = GeminiClient(model=_Model())
        clients.append(client)
        return client

    monkeypatch.setattr(analyze_module, "GeminiClient", create_client)
    state = CoordinatorState(
        event_id="db24a9c6-3377-4da1-a3a8-6b04503c7e71",
        event={"name": "Gala", "requirements": "Vegetarian catering"},
    )

    result = asyncio.run(analyze_module.analyze_requirements(state))

    assert result == {"requirements_analysis": "A detailed event analysis."}
    client = clients[0]
    assert client.operation_timeout == 1800
    assert client.coordinator_operation_timeout == 1800


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


def test_vendor_analysis_uses_application_json_and_parses_nullable_service_id() -> None:
    model = _VendorResponseModel()
    client = GeminiClient(model=model)

    response = asyncio.run(
        client.generate_with_prompt(
            "Rank the supplied candidate vendors.",
            VendorRecommendationResponse,
            node_name="vendor_analysis_recommend",
        )
    )

    assert model.generation_config is not None
    assert model.generation_config["response_mime_type"] == "application/json"
    assert model.generation_config["response_schema"]["properties"][
        "recommendations"
    ]["type_"] == "ARRAY"
    assert response.recommendations[0].vendor_service_id is None


def test_interactions_adapter_sends_gemini_38_structured_request() -> None:
    requests: list[dict[str, Any]] = []

    class FakeInteractions:
        async def create(self, **kwargs):
            requests.append(kwargs)
            return SimpleNamespace(
                output_text=json.dumps(
                    {
                        "recommendations": [
                            {
                                "vendorId": "00000000-0000-0000-0000-000000000001",
                                "vendorServiceId": None,
                                "score": 88,
                                "reasons": ["Matches the requested event category."],
                            }
                        ]
                    }
                )
            )

    adapter = gemini_client_module._GeminiInteractionsModel(
        model_name="gemini-3.8-flash",
        interactions_client=SimpleNamespace(
            aio=SimpleNamespace(interactions=FakeInteractions())
        ),
        streaming_model=_Model(),
    )

    response = asyncio.run(
        GeminiClient(model=adapter, timeout=2.5).generate_with_prompt(
            "Rank the supplied candidate vendors.",
            VendorRecommendationResponse,
            node_name="vendor_analysis_recommend",
        )
    )

    assert response.recommendations[0].vendor_service_id is None
    assert len(requests) == 1
    request = requests[0]
    assert request["model"] == "gemini-3.8-flash"
    assert request["response_format"]["type"] == "text"
    assert request["response_format"]["mime_type"] == "application/json"
    assert request["response_format"]["schema"]["type"] == "object"
    item_schema = request["response_format"]["schema"]["properties"][
        "recommendations"
    ]["items"]
    assert item_schema["properties"]["vendorId"] == {"type": "string"}
    assert item_schema["properties"]["vendorServiceId"] == {
        "type": "string",
        "nullable": True,
    }
    assert "minItems" not in request["response_format"]["schema"]["properties"][
        "recommendations"
    ]
    assert "maxItems" not in request["response_format"]["schema"]["properties"][
        "recommendations"
    ]
    assert request["store"] is False
    assert request["timeout"] == 2500


def test_configured_interactions_client_disables_sdk_managed_retries(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    captured: dict[str, Any] = {}

    monkeypatch.setattr(gemini_client_module.genai, "GenerativeModel", lambda _name: _Model())

    def create_v2_client(**kwargs: Any) -> Any:
        captured.update(kwargs)
        return SimpleNamespace()

    monkeypatch.setattr(gemini_client_module.genai_v2, "Client", create_v2_client)

    gemini_client_module._configured_model.__wrapped__(
        "gemini-3.8-flash",
        "test-api-key",
    )

    assert captured["http_options"].retry_options.attempts == 1


def test_interactions_bad_request_becomes_non_retryable_invalid_argument() -> None:
    class FakeInteractions:
        async def create(self, **_kwargs):
            raise gemini_client_module.genai_v2.errors.APIError(
                400,
                {"error": {"message": "invalid response schema"}},
            )

    adapter = gemini_client_module._GeminiInteractionsModel(
        model_name="gemini-3.8-flash",
        interactions_client=SimpleNamespace(
            aio=SimpleNamespace(interactions=FakeInteractions())
        ),
        streaming_model=_Model(),
    )

    with pytest.raises(GeminiInvalidRequestError):
        asyncio.run(
            GeminiClient(model=adapter).generate_with_prompt(
                "Rank the supplied candidate vendors.",
                VendorRecommendationResponse,
            )
        )


def test_interactions_403_quota_reason_is_converted_to_resource_exhausted() -> None:
    class FakeInteractions:
        async def create(self, **_kwargs):
            raise gemini_client_module.genai_v2.errors.APIError(
                403,
                {
                    "error": {
                        "status": "RESOURCE_EXHAUSTED",
                        "message": "Quota exceeded for this project.",
                    }
                },
            )

    adapter = gemini_client_module._GeminiInteractionsModel(
        model_name="gemini-3.8-flash",
        interactions_client=SimpleNamespace(
            aio=SimpleNamespace(interactions=FakeInteractions())
        ),
        streaming_model=_Model(),
    )

    with pytest.raises(ResourceExhausted):
        asyncio.run(
            adapter.generate_content_async(
                "Prompt",
                generation_config={
                    "response_mime_type": "application/json",
                    "response_schema": pydantic_to_gemini_schema(AnalysisOutput),
                },
            )
        )


@pytest.mark.parametrize(
    ("http_status", "expected_error", "key_status", "primary_attempts"),
    [
        (429, ResourceExhausted, "CoolingDown", 1),
        (500, InternalServerError, "Unavailable", 2),
        (502, ServiceUnavailable, "Unavailable", 2),
        (503, ServiceUnavailable, "Unavailable", 2),
        (504, DeadlineExceeded, "Unavailable", 2),
    ],
)
def test_interactions_status_retries_server_errors_once_then_rotates(
    monkeypatch: pytest.MonkeyPatch,
    caplog: pytest.LogCaptureFixture,
    http_status: int,
    expected_error: type[Exception],
    key_status: str,
    primary_attempts: int,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key=f"interactions-{http_status}-key-1",
        gemini_api_keys=f"interactions-{http_status}-key-2",
        gemini_model="gemini-primary",
        gemini_max_retries=5,
    )
    calls: list[str] = []
    delays: list[float] = []
    failure_types: list[type[Exception]] = []

    class FakeInteractions:
        def __init__(self, api_key: str) -> None:
            self.api_key = api_key

        async def create(self, **_kwargs: Any) -> Any:
            calls.append(self.api_key)
            if self.api_key == settings.get_gemini_api_keys()[0]:
                raise gemini_client_module.genai_v2.errors.APIError(
                    http_status,
                    {"error": {"message": f"HTTP {http_status} provider failure"}},
                )
            return SimpleNamespace(output_text='{"analysis": "recovered"}')

    def create_model(_model_name: str, api_key: str) -> Any:
        return gemini_client_module._GeminiInteractionsModel(
            model_name="gemini-primary",
            interactions_client=SimpleNamespace(
                aio=SimpleNamespace(interactions=FakeInteractions(api_key))
            ),
            streaming_model=_Model(),
        )

    async def record_delay(delay: float) -> None:
        delays.append(delay)

    record_failed_key = GeminiClient._record_failed_key

    def capture_failed_key(
        self: GeminiClient,
        api_key_index: int,
        *,
        health_status: str,
        reason: str,
        error: Exception,
        node_name: str,
        model_name: str,
        key_order: tuple[int, ...],
    ) -> None:
        failure_types.append(type(error))
        record_failed_key(
            self,
            api_key_index,
            health_status=health_status,
            reason=reason,
            error=error,
            node_name=node_name,
            model_name=model_name,
            key_order=key_order,
        )

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)
    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", record_delay)
    monkeypatch.setattr(GeminiClient, "_record_failed_key", capture_failed_key)
    caplog.set_level(logging.INFO)

    result = asyncio.run(
        GeminiClient().generate_with_prompt(
            f"Rotate on Interactions API HTTP {http_status}.",
            AnalysisOutput,
            node_name=f"interactions_{http_status}",
        )
    )

    assert result.analysis == "recovered"
    assert calls == [
        *([settings.get_gemini_api_keys()[0]] * primary_attempts),
        settings.get_gemini_api_keys()[1],
    ]
    assert delays == []
    assert failure_types == [expected_error]
    assert caplog.text.count("Retrying Gemini key 1 once") == primary_attempts - 1
    assert f"Gemini key 1 failed with {http_status}" in caplog.text
    assert "Rotating to Gemini key 2" in caplog.text
    assert "Gemini key 2 succeeded" in caplog.text
    health = gemini_client_module._get_gemini_key_pool(
        settings.get_gemini_api_keys()
    ).health_snapshot()
    assert health[0]["status"] == key_status
    assert health[0]["failure_count"] == 1
    assert health[0]["last_failure"]
    assert health[0]["cooldown_expiry"]
    assert health[1]["status"] == "Healthy"
    assert health[1]["last_success"]


def test_503_retry_then_key_rotation_completes_under_five_seconds(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="slow-503-primary",
        gemini_api_keys="slow-503-secondary",
        gemini_model="gemini-primary",
    )
    calls: list[str] = []

    class FakeInteractions:
        def __init__(self, api_key: str) -> None:
            self.api_key = api_key

        async def create(self, **_kwargs: Any) -> Any:
            calls.append(self.api_key)
            if self.api_key == settings.get_gemini_api_keys()[0]:
                await asyncio.sleep(1.6)
                raise gemini_client_module.genai_v2.errors.APIError(
                    503,
                    {"error": {"message": "Service unavailable"}},
                )
            return SimpleNamespace(output_text='{"analysis": "recovered"}')

    def create_model(_model_name: str, api_key: str) -> Any:
        return gemini_client_module._GeminiInteractionsModel(
            model_name="gemini-primary",
            interactions_client=SimpleNamespace(
                aio=SimpleNamespace(interactions=FakeInteractions(api_key))
            ),
            streaming_model=_Model(),
        )

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    started = time.perf_counter()
    result = asyncio.run(
        GeminiClient().generate_with_prompt(
            "Measure 503 retry and rotation elapsed time.",
            AnalysisOutput,
            node_name="measured_503_failover",
        )
    )
    elapsed = time.perf_counter() - started

    assert result.analysis == "recovered"
    assert calls == [
        "slow-503-primary",
        "slow-503-primary",
        "slow-503-secondary",
    ]
    assert elapsed < 5


def test_malformed_gemini_schema_fails_before_provider_call() -> None:
    class FreeFormResponse(BaseModel):
        values: dict[str, str]

    model = _Model()
    client = GeminiClient(model=model)

    with pytest.raises(GeminiSchemaError, match="cannot be represented"):
        asyncio.run(
            client.generate_with_prompt("Return structured data.", FreeFormResponse)
        )

    assert model.call_count == 0


def test_vendor_schema_omits_gemini_unsupported_uuid_format() -> None:
    response_schema = pydantic_to_gemini_schema(VendorRecommendationResponse)
    recommendations_schema = response_schema["properties"]["recommendations"]
    item_properties = recommendations_schema["items"]["properties"]
    vendor_id_schema = response_schema["properties"]["recommendations"]["items"][
        "properties"
    ]["vendorId"]

    assert "recommendations" in response_schema["required"]
    assert recommendations_schema["type_"] == "ARRAY"
    assert recommendations_schema["items"]["type_"] == "OBJECT"
    assert set(recommendations_schema["items"]["required"]) == {
        "vendorId",
        "score",
        "reasons",
    }
    assert vendor_id_schema["type_"] == "STRING"
    assert "format_" not in vendor_id_schema
    assert item_properties["vendorServiceId"]["type_"] == "STRING"
    assert item_properties["vendorServiceId"]["nullable"] is True
    assert item_properties["score"]["type_"] == "INTEGER"
    assert item_properties["reasons"]["type_"] == "ARRAY"
    assert item_properties["reasons"]["items"]["type_"] == "STRING"
    protos.Schema(response_schema)


def test_invalid_argument_is_not_retried_across_fallbacks() -> None:
    model = _InvalidArgumentModel()
    client = GeminiClient(model=model, max_retries=3)

    with pytest.raises(GeminiInvalidRequestError, match="structured request arguments"):
        asyncio.run(
            client.generate_with_prompt(
                "Rank the supplied candidate vendors.", VendorRecommendationResponse
            )
        )

    assert model.call_count == 1


def test_invalid_argument_does_not_rotate_to_another_api_key(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_api_keys="secondary-test-key",
        gemini_model="gemini-primary",
        gemini_fallback_models="",
    )
    calls: list[str] = []

    def create_model(_model_name: str, api_key: str) -> _InvalidArgumentModel:
        calls.append(api_key)
        return _InvalidArgumentModel()

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    with pytest.raises(GeminiInvalidRequestError):
        asyncio.run(
            GeminiClient(max_retries=3).generate_with_prompt(
                "Rank the supplied candidate vendors.", VendorRecommendationResponse
            )
        )

    assert calls == ["primary-test-key"]


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


def test_gemini_client_never_uses_a_provider_timeout_below_30_minutes() -> None:
    client = GeminiClient(model=_DeadlineModel())

    assert client.timeout == 1800


def test_gemini_deadline_is_reported_as_timeout() -> None:
    client = GeminiClient(model=_DeadlineModel(), max_retries=0)

    with pytest.raises(GeminiTimeoutError):
        asyncio.run(
            client.generate_with_prompt(
                "Report a timeout for this unique request.", AnalysisOutput
            )
        )


def test_gemini_operation_deadline_cancels_a_hanging_request() -> None:
    client = GeminiClient(
        model=_HangingModel(),
        timeout=10,
        operation_timeout=0.01,
        max_retries=0,
    )

    with pytest.raises(GeminiTimeoutError):
        asyncio.run(
            client.generate_with_prompt(
                "Cancel a hanging request at the operation deadline.",
                AnalysisOutput,
            )
        )


def test_coordinator_node_uses_the_initialized_coordinator_timeout(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_timeout_seconds=2,
        gemini_max_retries=0,
        coordinator_timeout_seconds=1,
        max_iterations=1,
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    original_wait_for = asyncio.wait_for
    observed_timeouts: list[float] = []

    async def observe_wait_for(awaitable: Any, timeout: float) -> Any:
        observed_timeouts.append(timeout)
        return await original_wait_for(awaitable, timeout)

    monkeypatch.setattr(gemini_client_module.asyncio, "wait_for", observe_wait_for)
    client = GeminiClient(model=_Model())

    asyncio.run(
        client.generate_with_prompt(
            "Use the coordinator's bounded per-node timeout.",
            AnalysisOutput,
            node_name="assess_risks",
        )
    )

    assert observed_timeouts == [pytest.approx(1800)]


def test_gemini_operation_deadline_cancels_a_hanging_stream() -> None:
    client = GeminiClient(
        model=_HangingStreamModel(),
        timeout=10,
        operation_timeout=0.01,
        max_retries=0,
    )

    async def consume_stream() -> None:
        async for _ in client.generate_plan_with_streaming(
            {"name": "Gala"},
            CoordinatorPlanOutput,
        ):
            pass

    with pytest.raises(GeminiTimeoutError):
        asyncio.run(consume_stream())


def test_invalid_model_identifier_fails_settings_validation() -> None:
    with pytest.raises(ValidationError):
        Settings(_env_file=None, gemini_model="not-a-gemini-model")


def test_gemini_model_is_loaded_from_environment(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.delenv("GEMINI_API_KEY", raising=False)
    monkeypatch.setenv("GEMINI_MODEL", "gemini-3.8-flash")
    monkeypatch.setenv("GEMINI_API_KEYS", "second-key, third-key")
    monkeypatch.setenv("GEMINI_FALLBACK_MODELS", "gemini-2.5-flash-lite,gemini-2.0-flash")

    settings = Settings(_env_file=None)

    assert settings.gemini_model == "gemini-3.8-flash"
    assert settings.get_gemini_api_keys() == ("second-key", "third-key")
    assert settings.get_gemini_models() == (
        "gemini-3.8-flash",
        "gemini-2.5-flash-lite",
        "gemini-2.0-flash",
    )


def test_default_gemini_fallback_chain_uses_supported_models(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.delenv("GEMINI_MODEL", raising=False)
    monkeypatch.delenv("GEMINI_FALLBACK_MODELS", raising=False)

    settings = Settings(_env_file=None)

    assert settings.get_gemini_models() == (
        "gemini-3.8-flash",
        "gemini-2.5-flash-lite",
        "gemini-2.0-flash",
    )
    assert "gemini-2.5-flash" not in settings.get_gemini_models()


def test_primary_model_succeeds_without_fallback(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_model="gemini-primary",
        gemini_fallback_models="gemini-fallback",
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    calls: list[str] = []

    def create_model(model_name: str, _api_key: str) -> _Model:
        calls.append(model_name)
        return _Model()

    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    result = asyncio.run(
        GeminiClient().generate_with_prompt(
            "Use the primary model for this unique request.",
            AnalysisOutput,
            node_name="test_primary_success",
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert calls == ["gemini-primary"]


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


def test_concurrent_requests_round_robin_across_configured_keys(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="pool-key-1",
        gemini_api_keys="pool-key-2,pool-key-3,pool-key-4",
        gemini_model="gemini-primary",
        gemini_max_retries=0,
    )
    selected_keys: list[str] = []

    def create_model(_model_name: str, api_key: str) -> _Model:
        selected_keys.append(api_key)
        return _Model()

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    async def generate_many() -> list[AnalysisOutput]:
        clients = [GeminiClient() for _ in range(4)]
        return await asyncio.gather(
            *(
                client.generate_with_prompt(
                    f"Concurrent request {index} for key-pool distribution.",
                    AnalysisOutput,
                    node_name=f"concurrent_{index}",
                )
                for index, client in enumerate(clients)
            )
        )

    responses = asyncio.run(generate_many())

    assert all(response.analysis == "A detailed event analysis." for response in responses)
    assert set(selected_keys) == {
        "pool-key-1",
        "pool-key-2",
        "pool-key-3",
        "pool-key-4",
    }
    assert len(selected_keys) == 4


def test_quarantined_key_is_skipped_and_returns_after_cooldown(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    pool = gemini_client_module._get_gemini_key_pool(("cooldown-key-1", "cooldown-key-2"))
    failed_at, cooldown_until = pool.quarantine(0, "rate_limit")
    assert failed_at
    assert cooldown_until
    assert pool.ordered_available_indices() == (1,)

    now = gemini_client_module.time.monotonic()
    monkeypatch.setattr(
        gemini_client_module.time,
        "monotonic",
        lambda: now + gemini_client_module._GEMINI_KEY_COOLDOWN_SECONDS + 1,
    )

    assert pool.ordered_available_indices() == (0, 1)


def test_unavailable_key_returns_to_healthy_after_cooldown(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    pool = gemini_client_module._get_gemini_key_pool(
        ("unavailable-cooldown-key-1", "unavailable-cooldown-key-2")
    )
    pool.record_failure(
        0,
        reason="503",
        status="Unavailable",
    )
    assert pool.health_snapshot()[0]["status"] == "Unavailable"
    now = gemini_client_module.time.monotonic()
    monkeypatch.setattr(
        gemini_client_module.time,
        "monotonic",
        lambda: now + gemini_client_module._GEMINI_KEY_COOLDOWN_SECONDS + 1,
    )

    assert pool.ordered_available_indices() == (0, 1)
    assert pool.health_snapshot()[0]["status"] == "Healthy"


def test_unavailable_primary_model_falls_back_to_next_model(
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
        return _NotFoundModel() if model_name == "gemini-primary" else _Model()

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


def test_all_unavailable_models_have_model_domain_error(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_model="gemini-unavailable",
        gemini_fallback_models="gemini-also-unavailable",
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(
        gemini_client_module,
        "_configured_model",
        lambda *_args: _NotFoundModel(),
    )

    with pytest.raises(GeminiInvalidModelError):
        asyncio.run(
            GeminiClient().generate_with_prompt(
                "Classify unavailable models for this request.",
                AnalysisOutput,
            )
        )


def test_transient_errors_fail_over_without_same_key_backoff(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_api_keys="secondary-test-key",
        gemini_model="gemini-primary",
        gemini_max_retries=5,
    )
    model = _RetryModel(failures=2)
    calls: list[str] = []
    delays: list[float] = []

    def create_model(_model_name: str, api_key: str) -> _Model:
        calls.append(api_key)
        return model if api_key == "primary-test-key" else _Model()

    async def record_delay(delay: float) -> None:
        delays.append(delay)

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)
    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", record_delay)

    result = asyncio.run(
        GeminiClient(retry_delay=1.0).generate_with_prompt(
            "Retry with exponential backoff for this unique request.",
            AnalysisOutput,
            node_name="test_backoff",
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert calls == ["primary-test-key", "secondary-test-key"]
    assert model.attempt_count == 2
    assert delays == []


@pytest.mark.parametrize(
    "failure",
    [
        httpx.ConnectError("connection failed"),
        httpx.ReadTimeout("request timed out"),
        ConnectionResetError("connection reset"),
        OSError("socket unavailable"),
    ],
)
def test_transport_failures_rotate_to_another_healthy_key(
    monkeypatch: pytest.MonkeyPatch,
    failure: Exception,
) -> None:
    suffix = type(failure).__name__
    settings = Settings(
        _env_file=None,
        gemini_api_key=f"transport-{suffix}-primary",
        gemini_api_keys=f"transport-{suffix}-secondary",
        gemini_model="gemini-primary",
        gemini_max_retries=5,
    )
    calls: list[str] = []
    delays: list[float] = []

    def create_model(_model_name: str, api_key: str) -> _Model:
        calls.append(api_key)
        return (
            _TransportFailureModel(failure)
            if api_key == settings.get_gemini_api_keys()[0]
            else _Model()
        )

    async def record_delay(delay: float) -> None:
        delays.append(delay)

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)
    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", record_delay)

    result = asyncio.run(
        GeminiClient().generate_with_prompt(
            f"Fail over on {suffix}.",
            AnalysisOutput,
            node_name=f"transport_{suffix}",
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert calls == list(settings.get_gemini_api_keys())
    assert delays == []
    health = gemini_client_module._get_gemini_key_pool(
        settings.get_gemini_api_keys()
    ).health_snapshot()
    assert health[0]["status"] == "CoolingDown"
    assert health[0]["failure_count"] == 1
    assert health[1]["status"] == "Healthy"
    assert health[1]["last_success"]


def test_provider_timeout_rotates_before_the_operation_deadline(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="timeout-failover-key-1",
        gemini_api_keys="timeout-failover-key-2",
        gemini_model="gemini-primary",
    )
    calls: list[str] = []

    def create_model(_model_name: str, api_key: str) -> _Model:
        calls.append(api_key)
        return _HangingModel() if api_key == settings.get_gemini_api_keys()[0] else _Model()

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    result = asyncio.run(
        GeminiClient(timeout=1, operation_timeout=0.2).generate_with_prompt(
            "Rotate after the first key times out.",
            AnalysisOutput,
            node_name="timeout_failover",
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert calls == list(settings.get_gemini_api_keys())
    health = gemini_client_module._get_gemini_key_pool(
        settings.get_gemini_api_keys()
    ).health_snapshot()
    assert health[0]["status"] == "CoolingDown"
    assert health[1]["last_success"]


def test_slow_successful_generation_keeps_its_configured_timeout(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="slow-success-primary",
        gemini_api_keys="slow-success-secondary",
        gemini_model="gemini-primary",
    )
    calls: list[str] = []

    class _SlowSuccessfulModel(_Model):
        async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> Any:
            calls.append("slow-success-primary")
            await asyncio.sleep(2.1)
            return SimpleNamespace(
                text='{"analysis": "slow response recovered"}'
            )

    def create_model(_model_name: str, api_key: str) -> _Model:
        if api_key == settings.get_gemini_api_keys()[0]:
            return _SlowSuccessfulModel()
        return _Model()

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    result = asyncio.run(
        GeminiClient(timeout=3, operation_timeout=10).generate_with_prompt(
            "Keep a valid slow response on its original key.",
            AnalysisOutput,
        )
    )

    assert result.analysis == "slow response recovered"
    assert calls == ["slow-success-primary"]


def test_rate_limit_immediately_fails_over_without_sleeping(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_api_keys="secondary-test-key",
        gemini_model="gemini-primary",
        gemini_max_retries=1,
    )
    calls: list[str] = []
    delays: list[float] = []

    async def record_delay(delay: float) -> None:
        delays.append(delay)

    def create_model(_model_name: str, api_key: str) -> _Model:
        calls.append(api_key)
        return _RateLimitModel() if api_key == "primary-test-key" else _Model()

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)
    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", record_delay)

    result = asyncio.run(
        GeminiClient(retry_delay=1.0).generate_with_prompt(
            "Fail over immediately after this rate limit.",
            AnalysisOutput,
        )
    )

    assert result.analysis == "A detailed event analysis."
    assert calls == ["primary-test-key", "secondary-test-key"]
    assert delays == []


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

    with pytest.raises(GeminiQuotaError) as exc_info:
        asyncio.run(
            GeminiClient().generate_with_prompt(
                "Exhaust every configured fallback exactly once.",
                AnalysisOutput,
            )
        )

    assert calls == [
        ("gemini-primary", "primary-test-key"),
        ("gemini-primary", "secondary-test-key"),
    ]
    assert exc_info.value.error_code == "all_gemini_keys_exhausted"
    assert exc_info.value.available_keys == 0
    assert exc_info.value.retry_after == 600

    with pytest.raises(GeminiQuotaError) as second_call:
        asyncio.run(
            GeminiClient().generate_with_prompt(
                "Fail immediately when every configured key is quarantined.",
                AnalysisOutput,
            )
        )

    assert second_call.value.available_keys == 0
    assert len(calls) == 2


def test_all_twenty_one_unavailable_keys_get_one_retry_then_reported(
    monkeypatch: pytest.MonkeyPatch,
    caplog: pytest.LogCaptureFixture,
) -> None:
    api_keys = tuple(f"unavailable-test-key-{index}" for index in range(1, 22))
    settings = Settings(
        _env_file=None,
        gemini_api_key=api_keys[0],
        gemini_api_keys=",".join(api_keys[1:]),
        gemini_model="gemini-primary",
        gemini_max_retries=5,
    )
    calls: list[str] = []

    class _UnavailableModel(_Model):
        def __init__(self, api_key: str) -> None:
            super().__init__()
            self.api_key = api_key

        async def generate_content_async(self, *_args: Any, **_kwargs: Any) -> Any:
            calls.append(self.api_key)
            raise ServiceUnavailable("HTTP 503 unavailable")

    def create_model(_model_name: str, api_key: str) -> _Model:
        return _UnavailableModel(api_key)

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)
    caplog.set_level(logging.INFO)

    with pytest.raises(GeminiNetworkError):
        asyncio.run(
            GeminiClient().generate_with_prompt(
                "Exhaust every available key after HTTP 503.",
                AnalysisOutput,
                node_name="all_keys_503",
            )
        )

    assert calls == [key for api_key in api_keys for key in (api_key, api_key)]
    assert caplog.text.count("Retrying Gemini key") == 21
    assert "All 21 Gemini API keys exhausted" in caplog.text
    health = gemini_client_module._get_gemini_key_pool(api_keys).health_snapshot()
    assert all(item["status"] == "Unavailable" for item in health)
    assert all(item["failure_count"] == 1 for item in health)


def test_all_keys_quarantined_before_model_fallback_returns_exhausted(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_api_keys="secondary-test-key",
        gemini_model="gemini-quota",
        gemini_fallback_models="gemini-unavailable",
        gemini_max_retries=0,
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)

    def create_model(model_name: str, _api_key: str) -> _Model:
        return _QuotaModel() if model_name == "gemini-quota" else _NotFoundModel()

    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    with pytest.raises(GeminiQuotaError) as exc_info:
        asyncio.run(
            GeminiClient().generate_with_prompt(
                "Do not collapse mixed failures into quota.",
                AnalysisOutput,
            )
        )

    assert exc_info.value.error_code == "all_gemini_keys_exhausted"
    assert exc_info.value.available_keys == 0


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


def test_interactions_permission_error_marks_key_failed_and_rotates(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="permission-test-key-1",
        gemini_api_keys="permission-test-key-2",
        gemini_model="gemini-primary",
    )

    class FakeInteractions:
        def __init__(self, api_key: str) -> None:
            self.api_key = api_key

        async def create(self, **_kwargs: Any) -> Any:
            if self.api_key == settings.get_gemini_api_keys()[0]:
                raise gemini_client_module.genai_v2.errors.APIError(
                    403,
                    {"error": {"status": "PERMISSION_DENIED", "message": "forbidden"}},
                )
            return SimpleNamespace(output_text='{"analysis": "recovered"}')

    def create_model(_model_name: str, api_key: str) -> Any:
        return gemini_client_module._GeminiInteractionsModel(
            model_name="gemini-primary",
            interactions_client=SimpleNamespace(
                aio=SimpleNamespace(interactions=FakeInteractions(api_key))
            ),
            streaming_model=_Model(),
        )

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", create_model)

    result = asyncio.run(
        GeminiClient().generate_with_prompt(
            "Rotate after an Interactions API permission error.",
            AnalysisOutput,
            node_name="permission_failover",
        )
    )

    assert result.analysis == "recovered"
    health = gemini_client_module._get_gemini_key_pool(
        settings.get_gemini_api_keys()
    ).health_snapshot()
    assert health[0]["status"] == "Failed"
    assert health[0]["failure_count"] == 1
    assert health[0]["last_failure"]
    assert health[0]["cooldown_expiry"] is None
    assert health[1]["last_success"]


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


@pytest.mark.parametrize(
    ("failure", "domain_error"),
    [
        (httpx.ConnectError("connection failed"), GeminiNetworkError),
        (ConnectionError("connection failed"), GeminiNetworkError),
        (socket.gaierror(-2, "DNS lookup failed"), GeminiNetworkError),
        (httpx.ReadTimeout("request timed out"), GeminiTimeoutError),
    ],
)
def test_transport_failures_have_a_domain_error(
    monkeypatch: pytest.MonkeyPatch,
    failure: Exception,
    domain_error: type[GeminiClientError],
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_model="gemini-primary",
        gemini_max_retries=1,
    )
    model = _TransportFailureModel(failure)

    async def skip_delay(_delay: float) -> None:
        return None

    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    monkeypatch.setattr(gemini_client_module, "_configured_model", lambda *_args: model)
    monkeypatch.setattr(gemini_client_module.asyncio, "sleep", skip_delay)

    with pytest.raises(domain_error):
        asyncio.run(
            GeminiClient().generate_with_prompt(
                "Map transport failures to a Gemini domain error.",
                AnalysisOutput,
            )
        )

    assert model.attempt_count == 1


def test_startup_validation_does_not_spend_provider_quota(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setattr(
        gemini_client_module,
        "get_settings",
        lambda: Settings(
            _env_file=None, gemini_api_key="test-key", gemini_model="gemini-3.8-flash"
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
        lambda: Settings(_env_file=None, gemini_model="gemini-3.8-flash"),
    )
    monkeypatch.delenv("GEMINI_API_KEY", raising=False)
    monkeypatch.delenv("GEMINI_API_KEYS", raising=False)

    with pytest.raises(GeminiConfigurationError):
        configure_gemini()


def test_model_listing_diagnostic_uses_configured_key(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    settings = Settings(
        _env_file=None,
        gemini_api_key="primary-test-key",
        gemini_model="gemini-3.8-flash",
    )
    monkeypatch.setattr(gemini_client_module, "get_settings", lambda: settings)
    keys: list[str] = []

    def create_model_service_client(api_key: str) -> object:
        keys.append(api_key)
        return object()

    monkeypatch.setattr(
        gemini_client_module,
        "_configured_model_service_client",
        create_model_service_client,
    )
    monkeypatch.setattr(
        gemini_client_module.genai,
        "list_models",
        lambda **_kwargs: [
            _ListedModel("models/gemini-3.8-flash"),
            _ListedModel("models/gemini-3.7-flash"),
            _ListedModel("models/gemini-3.8-flash"),
        ],
    )

    assert list_available_gemini_models() == (
        "gemini-3.8-flash",
        "gemini-3.7-flash",
    )
    assert keys == ["primary-test-key"]
