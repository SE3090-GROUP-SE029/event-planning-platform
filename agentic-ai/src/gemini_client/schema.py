"""Convert Pydantic schemas to the supported Gemini structured-output subset."""

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
    if not isinstance(definitions, dict):
        raise ValueError("Gemini schema definitions must be an object")

    def convert(node: dict[str, Any], active_refs: frozenset[str]) -> dict[str, Any]:
        if not isinstance(node, dict):
            raise ValueError("Gemini schema nodes must be objects")
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
            if not isinstance(alternatives, list):
                raise ValueError("Gemini schema anyOf must be an array")
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
        if not isinstance(json_type, str):
            raise ValueError(f"Unsupported Gemini schema type: {json_type!r}")
        gemini_type = _SCHEMA_TYPES.get(json_type)
        if gemini_type is None:
            raise ValueError(f"Unsupported Gemini schema type: {json_type!r}")

        properties = node.get("properties")
        required = node.get("required", [])
        if json_type == "object":
            if not isinstance(properties, dict):
                raise ValueError("Gemini object schemas must define properties")
            if not isinstance(required, list) or not all(
                isinstance(name, str) for name in required
            ):
                raise ValueError("Gemini schema required fields must be an array of names")
            unknown_required = set(required) - properties.keys()
            if unknown_required:
                raise ValueError(
                    "Gemini schema required fields are not defined in properties: "
                    + ", ".join(sorted(unknown_required))
                )
        elif properties is not None or "required" in node:
            raise ValueError("Gemini properties and required fields need an object schema")

        if json_type == "array" and not isinstance(node.get("items"), dict):
            raise ValueError("Gemini array schemas must define an item schema")

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
                if json_key == "format" and node[json_key] == "uuid":
                    continue
                result[proto_key] = node[json_key]
        if "items" in node:
            if not isinstance(node["items"], dict):
                raise ValueError("Gemini schema array items must be an object schema")
            result["items"] = convert(node["items"], active_refs)
        if properties is not None:
            if not all(isinstance(value, dict) for value in properties.values()):
                raise ValueError("Gemini schema property definitions must be objects")
            result["properties"] = {
                name: convert(property_schema, active_refs)
                for name, property_schema in properties.items()
            }
        return result

    return convert(json_schema, frozenset())


def pydantic_to_gemini_json_schema(model: type[BaseModel]) -> dict[str, Any]:
    """Build the JSON Schema shape used by Gemini's Interactions response format."""

    return gemini_schema_to_json_schema(pydantic_to_gemini_schema(model))


def gemini_schema_to_json_schema(schema: dict[str, Any]) -> dict[str, Any]:
    """Convert a validated protobuf-style Gemini schema to canonical JSON Schema."""

    return _legacy_schema_to_json_schema(schema)


def _legacy_schema_to_json_schema(schema: dict[str, Any]) -> dict[str, Any]:
    """Convert protobuf-style schema names into canonical JSON Schema names."""

    type_names = {
        "ARRAY": "array",
        "BOOLEAN": "boolean",
        "INTEGER": "integer",
        "NUMBER": "number",
        "OBJECT": "object",
        "STRING": "string",
    }
    converted: dict[str, Any] = {}
    for name, value in schema.items():
        if name == "type_":
            converted["type"] = type_names[value]
        elif name == "format_":
            converted["format"] = value
        elif name == "items":
            converted["items"] = _legacy_schema_to_json_schema(value)
        elif name == "properties":
            converted["properties"] = {
                property_name: _legacy_schema_to_json_schema(property_schema)
                for property_name, property_schema in value.items()
            }
        elif name in {"min_items", "max_items"}:
            continue
        else:
            converted[name] = value
    return converted
