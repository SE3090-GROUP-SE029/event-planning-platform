import asyncio
import json
from typing import Any

import pytest
from google.api_core.exceptions import DeadlineExceeded, NotFound
from google.generativeai import protos
from pydantic import ValidationError

from src.coordinator_agent.config import Settings, get_settings
from src.coordinator_agent.models import CoordinatorPlanOutput
from src.coordinator_agent.nodes.analyze import AnalysisOutput
from src.coordinator_agent.nodes.assess import BudgetOutput
from src.gemini_client import client as gemini_client_module
from src.gemini_client.client import GeminiClient, configure_gemini
from src.gemini_client.exceptions import (
    GeminiClientError,
    GeminiConfigurationError,
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
        self.token_request_options: dict[str, Any] | None = None

    def count_tokens(self, prompt: str, **_kwargs: Any) -> Any:
        return type("TokenCount", (), {"total_tokens": len(prompt) // 4})()

    async def count_tokens_async(self, prompt: str, **kwargs: Any) -> Any:
        self.token_request_options = kwargs["request_options"]
        return self.count_tokens(prompt)

    async def generate_content_async(self, *_args: Any, **kwargs: Any) -> _Response:
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


def test_client_generates_validated_plan_without_api_key() -> None:
    model = _Model()
    client = GeminiClient(model=model)
    plan = asyncio.run(client.generate_plan({"name": "Gala"}, CoordinatorPlanOutput))
    assert isinstance(plan, CoordinatorPlanOutput)
    assert model.token_request_options == {
        "timeout": get_settings().gemini_token_count_timeout_seconds
    }


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

    with pytest.raises(GeminiClientError):
        asyncio.run(client.generate_with_prompt("Analyze this event.", AnalysisOutput))

    assert model.call_count == 1


def test_gemini_deadline_is_reported_as_timeout() -> None:
    client = GeminiClient(model=_DeadlineModel(), max_retries=0)

    with pytest.raises(GeminiTimeoutError):
        asyncio.run(client.generate_with_prompt("Analyze this event.", AnalysisOutput))


def test_invalid_model_identifier_fails_settings_validation() -> None:
    with pytest.raises(ValidationError):
        Settings(gemini_model="not-a-gemini-model")


def test_gemini_model_is_loaded_from_environment(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setenv("GEMINI_MODEL", "gemini-2.5-flash")

    assert Settings(_env_file=None).gemini_model == "gemini-2.5-flash"


def test_startup_validation_accepts_accessible_generate_content_model(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setattr(
        gemini_client_module,
        "get_settings",
        lambda: Settings(gemini_api_key="test-key", gemini_model="gemini-2.5-flash"),
    )
    monkeypatch.setattr(gemini_client_module.genai, "configure", lambda **_kwargs: None)
    monkeypatch.setattr(
        gemini_client_module.genai,
        "list_models",
        lambda **_kwargs: iter(
            [
                type(
                    "ModelInfo",
                    (),
                    {
                        "name": "models/gemini-2.5-flash",
                        "supported_generation_methods": ["generateContent"],
                    },
                )()
            ]
        ),
    )

    configure_gemini()


def test_startup_validation_rejects_model_without_generate_content(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setattr(
        gemini_client_module,
        "get_settings",
        lambda: Settings(gemini_api_key="test-key", gemini_model="gemini-2.5-flash"),
    )
    monkeypatch.setattr(gemini_client_module.genai, "configure", lambda **_kwargs: None)
    monkeypatch.setattr(
        gemini_client_module.genai,
        "list_models",
        lambda **_kwargs: iter(
            [
                type(
                    "ModelInfo",
                    (),
                    {
                        "name": "models/gemini-2.5-flash",
                        "supported_generation_methods": ["embedContent"],
                    },
                )()
            ]
        ),
    )

    with pytest.raises(GeminiConfigurationError, match="does not support generateContent"):
        configure_gemini()


def test_startup_validation_rejects_unavailable_model(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    monkeypatch.setattr(
        gemini_client_module,
        "get_settings",
        lambda: Settings(gemini_api_key="test-key", gemini_model="gemini-9.9-unavailable"),
    )
    monkeypatch.setattr(gemini_client_module.genai, "configure", lambda **_kwargs: None)
    monkeypatch.setattr(gemini_client_module.genai, "list_models", lambda **_kwargs: iter(()))

    with pytest.raises(GeminiConfigurationError, match="is unavailable"):
        configure_gemini()
