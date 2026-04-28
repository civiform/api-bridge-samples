# Bridge - Python

Reference implementation of the [CiviForm Bridge contract](../docs/howto.md) using Flask + `jsonschema`.

## Requirements

- Python 3.11+
- `pip`
- `jq` (only required to run `bin/test.sh`)

## Setup

```bash
cd python
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

Dependencies (pinned in `requirements.txt`):

- `flask==3.1.3`
- `jsonschema==4.26.0`

## Run

```bash
./bin/run.sh
# or, equivalently:
python app.py
```

The service listens on `http://0.0.0.0:5010`.

Quick smoke check:

```bash
curl -s http://localhost:5010/health-check | jq
curl -s http://localhost:5010/discovery   | jq
```

## Test

With the service running in another terminal:

```bash
./bin/test.sh
```

Override the target with `BASE_URL=http://host:port ./bin/test.sh`.

## Layout

| File            | Purpose                                            |
| --------------- | -------------------------------------------------- |
| `app.py`        | Flask app, route wiring, error handlers            |
| `services.py`   | Bridge slug dispatch and request/response envelope |
| `schemas.py`    | JSON Schemas + `/discovery` document               |
| `validation.py` | Request payload validation                         |
| `responses.py`  | RFC 9457 problem+json helpers                      |
| `data.py`       | In-memory sample customer records                  |
