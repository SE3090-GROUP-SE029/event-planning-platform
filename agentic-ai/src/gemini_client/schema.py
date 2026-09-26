"""Convert Pydantic JSON schemas to the subset supported by google-generativeai."""

from typing import Any

from pydantic import BaseModel

_SCHEMA_TYPES = {
    "array": "ARRAY",
    "boolean": "BOOLEAN",
    "integer": "INTEGER",
    "number": "NUMBER",
    "object": "OBJECT",
    "string": "STRING",
}
_SUPPORTED_KEYWORDS = {
    "description",
    "enum",
    "format",
    "items",
    "maxItems",
    "minItems",
    "nullable",
    "properties",
    "required",
    "type",
}
_IGNORED_KEYWORDS = {
    "$defs",
    "$schema",
    "default",
    "examples",
    "exclusiveMaximum",
    "exclusiveMinimum",
    "maximum",
    "maxLength",
    "minLength",
    "minimum",
    "multipleOf",
    "pattern",
    "title",
}


def pydantic_to_gemini_schema(model: type[BaseModel]) -> dict[str, Any]:
    """Build a schema accepted by the legacy Gemini SDK from a Pydantic model.

    Pydantic remains the authoritative validator; constraints that Gemini's
    protobuf schema cannot express are intentionally enforced after generation.
    """

    json_schema = model.model_json_schema()
    definitions = json_schema.get("$defs", {})

    def convert(node: dict[str, Any], active_refs: frozenset[str]) -> dict[str, Any]:
        reference = node.get("$ref")
        if reference is not None:
            if not isinstance(reference, str) or not reference.startswith("#/$defs/"):
                raise ValueError(f"Unsupported Gemini schema reference: {reference!r}")
            if reference in active_refs:
                raise ValueError(f"Recursive Gemini schema reference: {reference}")
            definition_name = reference.removeprefix("#/$defs/")
            definition = definitions.get(definition_name)
            if not isinstance(definition, dict):
                raise ValueError(f"Unresolved Gemini schema reference: {reference}")
            node = {**definition, **{key: value for key, value in node.items() if key != "$ref"}}
            active_refs = active_refs | {reference}

        alternatives = node.get("anyOf")
        if alternatives is not None:
            non_null = [
                alternative
                for alternative in alternatives
                if isinstance(alternative, dict) and alternative.get("type") != "null"
            ]
            if len(alternatives) != 2 or len(non_null) != 1:
                raise ValueError("Gemini schema supports only nullable two-branch unions")
            node = {**non_null[0], **{key: value for key, value in node.items() if key != "anyOf"}}
            node["nullable"] = True

        additional_properties = node.get("additionalProperties")
        if additional_properties not in (None, False):
            raise ValueError(
                "Gemini response schemas cannot express free-form object properties"
            )

        unsupported = set(node) - _SUPPORTED_KEYWORDS - _IGNORED_KEYWORDS - {
            "$ref",
            "anyOf",
            "additionalProperties",
        }
        if unsupported:
            raise ValueError(
                f"Unsupported Gemini schema keywords: {', '.join(sorted(unsupported))}"
            )

        json_type = node.get("type")
        gemini_type = _SCHEMA_TYPES.get(json_type)
        if gemini_type is None:
            raise ValueError(f"Unsupported Gemini schema type: {json_type!r}")

        result: dict[str, Any] = {"type_": gemini_type}
        for json_key, proto_key in (
            ("description", "description"),
            ("format", "format_"),
            ("nullable", "nullable"),
            ("enum", "enum"),
            ("required", "required"),
            ("maxItems", "max_items"),
            ("minItems", "min_items"),
        ):
            if json_key in node:
                result[proto_key] = node[json_key]
        if "items" in node:
            if not isinstance(node["items"], dict):
                raise ValueError("Gemini schema array items must be an object schema")
            result["items"] = convert(node["items"], active_refs)
        if "properties" in node:
            properties = node["properties"]
            if not isinstance(properties, dict):
                raise ValueError("Gemini schema properties must be an object")
            result["properties"] = {
                name: convert(property_schema, active_refs)
                for name, property_schema in properties.items()
            }
        return result

    return convert(json_schema, frozenset())
