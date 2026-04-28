"""Bridge request handlers.

Each bridge slug routes to a handler that validates the request payload,
performs a customer lookup, validates the response payload, and returns
either an ``ok`` envelope or a problem response.
"""

from data import find_customer, find_customer_by_id
from responses import ok, problem
from schemas import BRIDGE_SLUG, LOOKUP_BRIDGE_SLUG
from validation import validate_request, validate_response


def _validation_error(errors):
    """Return a 422 problem response listing request ``errors``."""
    return problem(
        422,
        "Validation Error",
        "Request payload failed validation",
        {"validation_errors": errors},
    )


def _respond(slug, payload):
    """Validate ``payload`` against ``slug``'s response schema and return it.

    On schema failure, returns a 500 problem response so the
    contract violation is surfaced rather than silently shipped.
    """
    errors = validate_response(slug, payload)
    if errors:
        return problem(
            500,
            "Internal Server Error",
            "Response payload failed validation",
            {"validation_errors": errors},
        )
    return ok(payload)


def _check_customer(slug, payload):
    """Handle the ``check-utility-customer`` bridge.

    Validates the request, looks up a customer by name/account/address,
    and returns ``{"eligible": bool}``.
    """
    errors = validate_request(slug, payload)
    if errors:
        return _validation_error(errors)

    match = find_customer(
        payload["first_name"],
        payload["last_name"],
        payload["account_number"],
        payload["address1"],
        payload["city"],
        payload["state"],
        payload["zip"],
    )
    return _respond(slug, {"eligible": match is not None})


def _lookup_customer(slug, payload):
    """Handle the ``lookup-utility-customer`` bridge.

    Validates the request, looks up a customer by ``customer_id``, and
    returns ``{"found": bool}``.
    """
    errors = validate_request(slug, payload)
    if errors:
        return _validation_error(errors)

    match = find_customer_by_id(payload["customer_id"])
    return _respond(slug, {"found": match is not None})


_HANDLERS = {
    BRIDGE_SLUG: _check_customer,
    LOOKUP_BRIDGE_SLUG: _lookup_customer,
}


def handle_bridge_request(slug, body):
    """Route a bridge request body to the handler registered for ``slug``.

    Returns 404 when ``slug`` is unknown, 400 when ``body`` is not a JSON
    object containing a ``payload`` key, and otherwise the handler's
    response.
    """
    handler = _HANDLERS.get(slug)
    if handler is None:
        return problem(404, "Not Found", f"Unknown bridge endpoint: {slug}")

    if not isinstance(body, dict) or "payload" not in body:
        return problem(
            400, "Bad Request", "Request body must be JSON with a 'payload' object"
        )

    return handler(slug, body.get("payload"))
