# Bridge - Node.js

Reference implementation of the [CiviForm Bridge contract](../docs/howto.md) using Express + Ajv.

## Requirements

- Node.js 20+ (see `engines` in `package.json`)
- npm (bundled with Node)
- `jq` (only required to run `bin/test.sh`)

## Setup

```bash
cd node
npm install
```

Dependencies (pinned in `package.json`):

- `express@5.2.1`
- `ajv@8.20.0`
- `ajv-formats@3.0.1`

## Run

```bash
./bin/run.sh
# or, equivalently:
npm start
```

The service listens on `http://0.0.0.0:5011`. Override with `PORT=<port> npm start`.

Quick smoke check:

```bash
curl -s http://localhost:5011/health-check | jq
curl -s http://localhost:5011/discovery   | jq
```

## Test

With the service running in another terminal:

```bash
BASE_URL=http://localhost:5011 ./bin/test.sh
```

`BASE_URL` defaults to `http://localhost:5010` in the shared script, so set it explicitly when targeting the Node service.

## Layout

| File            | Purpose                                            |
| --------------- | -------------------------------------------------- |
| `app.js`        | Express app, route wiring, error handlers          |
| `services.js`   | Bridge slug dispatch and request/response envelope |
| `schemas.js`    | JSON Schemas + `/discovery` document               |
| `validation.js` | Ajv-based request payload validation               |
| `responses.js`  | RFC 9457 problem+json helpers                      |
| `data.js`       | In-memory sample customer records                  |
