"""Validation and privacy-safe diagnostics for ADK Gemini requests."""

import json
import logging
from typing import Any

from google.adk.agents.callback_context import CallbackContext
from google.adk.models.llm_request import LlmRequest
from google.genai import types
from pydantic import BaseModel

logger = logging.getLogger(__name__)

_SCHEMA_FIELDS = {
    field_name
    for name, field in types.Schema.model_fields.items()
    for field_name in (name, field.alias)
    if field_name is not None
} - {"additional_properties", "additionalProperties"}
_SCHEMA_FIELDS.update({"$defs", "$ref", "$schema"})
_SCHEMA_CHILD_FIELDS = {
    "items",
    "properties",
    "anyOf",
    "any_of",
    "oneOf",
    "one_of",
    "allOf",
    "all_of",
    "prefixItems",
    "prefix_items",
    "$defs",
    "defs",
}


class AdkSchemaError(ValueError):
    """Raised when an ADK request contains a Gemini-incompatible schema."""


def _schema_dict(schema: Any) -> dict[str, Any]:
    if isinstance(schema, type) and issubclass(schema, BaseModel):
        schema = schema.model_json_schema()
    if isinstance(schema, BaseModel):
        schema = schema.model_dump(mode="json", by_alias=True, exclude_none=True)
    if not isinstance(schema, dict):
        raise AdkSchemaError("Gemini response schema must be an object")
    return schema


def validate_gemini_schema(schema: Any) -> dict[str, Any]:
    """Validate the response schema shape before it reaches the provider."""

    schema_dict = _schema_dict(schema)

    def validate_node(node: Any, path: str) -> None:
        if isinstance(node, list):
            for index, value in enumerate(node):
                validate_node(value, f"{path}[{index}]")
            return
        if not isinstance(node, dict):
            return

        if "additional_properties" in node or "additionalProperties" in node:
            raise AdkSchemaError(
                f"Gemini response schema cannot contain additional-properties "
                f"settings at {path}"
            )
        unsupported = set(node) - _SCHEMA_FIELDS
        if unsupported:
            raise AdkSchemaError(
                f"Unsupported Gemini response-schema field at {path}: "
                + ", ".join(sorted(unsupported))
            )
        for key in _SCHEMA_CHILD_FIELDS:
            value = node.get(key)
            if key == "properties" and isinstance(value, dict):
                for property_name, child in value.items():
                    validate_node(child, f"{path}.properties.{property_name}")
            elif key in {"$defs", "defs"} and isinstance(value, dict):
                for definition_name, child in value.items():
                    validate_node(child, f"{path}.{key}.{definition_name}")
            elif value is not None:
                validate_node(value, f"{path}.{key}")

    validate_node(schema_dict, "response_schema")
    return schema_dict


def validate_and_log_adk_request(
    _callback_context: CallbackContext,
    llm_request: LlmRequest,
) -> None:
    """Reject unsafe schemas and log request structure without logging guest data."""

    schema = validate_gemini_schema(llm_request.config.response_schema)
    generation_config = llm_request.config.model_dump(
        mode="json",
        by_alias=True,
        exclude={"response_schema"},
        exclude_none=True,
    )
    generation_config.pop("systemInstruction", None)
    generation_config["responseSchema"] = schema

    safe_payload = {
        "model": llm_request.model,
        "contents": [
            {
                "role": content.role,
                "parts": [
                    {"text": "<redacted>"}
                    if part.text is not None
                    else {"type": type(part).__name__}
                    for part in content.parts or []
                ],
            }
            for content in llm_request.contents
        ],
        "generationConfig": generation_config,
    }
    logger.info(
        "ADK Gemini request model=%s generation_config=%s "
        "response_schema=%s serialized_request_payload=%s",
        llm_request.model,
        json.dumps(generation_config, ensure_ascii=True, sort_keys=True),
        json.dumps(schema, ensure_ascii=True, sort_keys=True),
        json.dumps(safe_payload, ensure_ascii=True, sort_keys=True),
    )
