"""JSON Schema validation for bridge request and response payloads."""

from jsonschema import Draft202012Validator

from schemas import (
    BRIDGE_SLUG,
    LOOKUP_BRIDGE_SLUG,
    LOOKUP_REQUEST_SCHEMA,
    LOOKUP_RESPONSE_SCHEMA,
    REQUEST_SCHEMA,
    RESPONSE_SCHEMA,
)

_REQUEST_VALIDATORS = {
    BRIDGE_SLUG: Draft202012Validator(REQUEST_SCHEMA),
    LOOKUP_BRIDGE_SLUG: Draft202012Validator(LOOKUP_REQUEST_SCHEMA),
}

_RESPONSE_VALIDATORS = {
    BRIDGE_SLUG: Draft202012Validator(RESPONSE_SCHEMA),
    LOOKUP_BRIDGE_SLUG: Draft202012Validator(LOOKUP_RESPONSE_SCHEMA),
}


def _field_name(error):
    """Derive the offending field name from a ``jsonschema`` ``ValidationError``.

    Falls back to parsing the message for ``required`` errors (which have
    no ``absolute_path``) and to ``"payload"`` when no field can be
    identified.
    """
    if error.absolute_path:
        return str(error.absolute_path[0])
    if error.validator == "required":
        return error.message.split("'")[1]
    return "payload"


def _format_errors(validator, payload):
    """Run ``validator`` over ``payload`` and return ``{name, message}`` dicts."""
    return [
        {"name": _field_name(error), "message": error.message}
        for error in validator.iter_errors(payload)
    ]


def validate_request(slug, payload):
    """Validate a request ``payload`` against the schema for ``slug``.

    Returns a list of ``{name, message}`` error dicts, empty on success.
    """
    return _format_errors(_REQUEST_VALIDATORS[slug], payload)


def validate_response(slug, payload):
    """Validate a response ``payload`` against the schema for ``slug``.

    Returns a list of ``{name, message}`` error dicts, empty on success.
    """
    return _format_errors(_RESPONSE_VALIDATORS[slug], payload)
