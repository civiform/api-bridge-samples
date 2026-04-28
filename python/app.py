"""Flask application entry point exposing the bridge HTTP endpoints."""

import time

from flask import Flask, jsonify, request

from schemas import discovery_document
from services import handle_bridge_request

app = Flask(__name__)


@app.get("/health-check")
def health_check():
    """Return the current server time as a liveness signal."""
    return jsonify({"timestamp": int(time.time())})


@app.get("/discovery")
def discovery():
    """Return the bridge discovery document describing every endpoint."""
    return jsonify(discovery_document())


@app.post("/bridge/<slug>")
def bridge(slug):
    """Dispatch a bridge request identified by ``slug`` to its handler."""
    return handle_bridge_request(slug, request.get_json(silent=True))


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5010)
