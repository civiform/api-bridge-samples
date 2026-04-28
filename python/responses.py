"""Flask response helpers for success and RFC 7807 problem responses."""

from flask import jsonify

from schemas import COMPATIBILITY_LEVEL

PROBLEM_CONTENT_TYPE = "application/problem+json"


def problem(status, title, detail, extra=None):
    """Build an RFC 7807 ``application/problem+json`` Flask response.

    Args:
        status: HTTP status code to set on the response.
        title: Short, human-readable summary of the problem type.
        detail: Human-readable explanation specific to this occurrence.
        extra: Optional mapping merged into the problem body (e.g. to
            attach ``validation_errors``).

    Returns:
        A Flask ``Response`` with the problem JSON body, status code,
        and ``application/problem+json`` content type.
    """
    body = {
        "type": "about:blank",
        "title": title,
        "status": status,
        "detail": detail,
    }
    if extra:
        body.update(extra)
    response = jsonify(body)
    response.status_code = status
    response.headers["Content-Type"] = PROBLEM_CONTENT_TYPE
    return response


def ok(payload):
    """Wrap ``payload`` in the standard success envelope and return it as JSON.

    The envelope includes the bridge ``compatibility_level`` so clients
    can detect contract changes.
    """
    return jsonify(
        {
            "compatibility_level": COMPATIBILITY_LEVEL,
            "payload": payload,
        }
    )
