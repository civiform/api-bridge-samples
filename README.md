# CiviForm Bridge Integration - Implementation Guide

A bridge service exposes a small HTTP+JSON surface that lets CiviForm:

1. Confirm the service is reachable (`/health-check`).
2. Discover which integration operations are available and what shape their data takes (`/discovery`).
3. Invoke a named operation with a JSON payload and receive a structured result (`/bridge/{slug}`).

Everything CiviForm needs to integrate can be found in the response from the `/discovery` endpoint. It includes operation names, request shape, response shape, and compatibility level. Treat `/discovery` as your public API contract.

Any programming language can be use. Any hosting option can be used as long as CiviForm can reach it.

---

## 1. Conventions

### Transport
- HTTPS (HTTP is acceptable only for local development).
- `Content-Type: application/json` for all success request and response bodies.
- `Content-Type: application/problem+json` for all error responses ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html)).
- UTF-8 encoding.

### Compatibility level
Every endpoint and every successful bridge response carries a `compatibility_level` string. The current level is `"v1"`. This is used by CiviForm to determine the correct integration method with the bridge API.

### Field naming
- **Top-level envelope fields** - (`compatibility_level`, `payload`, `endpoints`, `timestamp`, `validation_errors`, etc.) use `snake_case`.
- **Application data inside `payload`** -  (the keys CiviForm form authors map question answers to) uses `snake_case` by convention (e.g. `first_name`, `account_number`, `customer_id`). Use snake_case consistently in both the JSON Schemas you publish and the actual payloads you accept and return.
- **Exception to the naming rule** - The bridge url fragments (`/bridge/<slug>`) are in kebob case (`discount-program`).

---

## 2. Endpoints

### 2.1 `GET /health-check`

A liveness probe. No request body.

**200 response:**
```json
{ "timestamp": 1734200000 }
```
- `timestamp` - integer Unix epoch seconds at the moment the response was generated. Required.

---

### 2.2 `GET /discovery`

Returns the catalog of operations this service exposes. CiviForm calls this once at configuration time and uses the published JSON Schemas to render and validate question mappings.

The response body is a JSON object with a single top-level `endpoints` field - a map whose keys are the full bridge URI (`/bridge/{slug}`) and whose values describe the operation. One entry is emitted per operation the service supports. Each entry must contain:

- `compatibility_level` - contract version string (currently `"v1"`).
- `description` - short human-readable sentence describing the operation's purpose.
- `uri` - the POST path for this operation; must equal the map key.
- `request_schema` - JSON Schema 2020-12 document describing the `payload` that callers must send.
- `response_schema` - JSON Schema 2020-12 document describing the `payload` returned on success.

The slug embedded in the map key (the part after `/bridge/`) is the operation's stable identifier. It must be URL-safe, kebab-case, and unique within the service.

**200 response - shape:**
```json
{
  "endpoints": {
    "/bridge/<slug>": {
      "compatibility_level": "v1",
      "description": "Human-readable explanation of what this operation does.",
      "uri": "/bridge/<slug>",
      "request_schema":  { /* JSON Schema 2020-12 document */ },
      "response_schema": { /* JSON Schema 2020-12 document */ }
    }
  }
}
```

**200 response - sample (one operation: `check-utility-customer`):**
```json
{
  "endpoints": {
    "/bridge/check-utility-customer": {
      "compatibility_level": "v1",
      "description": "Check whether an applicant is an existing utility customer by name, account number, and address.",
      "uri": "/bridge/check-utility-customer",
      "request_schema": {
        "$schema": "https://json-schema.org/draft/2020-12/schema",
        "$id": "https://civiform.us/schemas/check-utility-customer-request.json",
        "title": "Check Utility Customer Request",
        "description": "Applicant-supplied identity and address fields used to look up a utility customer record.",
        "type": "object",
        "properties": {
          "first_name":     { "type": "string", "title": "First Name",     "description": "Applicant's legal first name." },
          "last_name":      { "type": "string", "title": "Last Name",      "description": "Applicant's legal last name." },
          "account_number": { "type": "string", "title": "Account Number", "description": "Utility account number as printed on a recent bill." },
          "address1":       { "type": "string", "title": "Address Line 1", "description": "Street address of the service location." },
          "city":           { "type": "string", "title": "City",           "description": "City of the service location." },
          "state":          { "type": "string", "title": "State",          "description": "Two-letter state code of the service location." },
          "zip":            { "type": "string", "title": "ZIP Code",       "description": "Postal ZIP code of the service location." }
        },
        "required": ["first_name", "last_name", "account_number", "address1", "city", "state", "zip"],
        "additionalProperties": false
      },
      "response_schema": {
        "$schema": "https://json-schema.org/draft/2020-12/schema",
        "$id": "https://civiform.us/schemas/check-utility-customer-response.json",
        "title": "Check Utility Customer Response",
        "description": "Eligibility decision for a utility customer lookup.",
        "type": "object",
        "properties": {
          "eligible": { "type": "boolean", "title": "Eligible", "description": "True when the applicant matches a utility customer record." }
        },
        "required": ["eligible"],
        "additionalProperties": false
      }
    }
  }
}
```

---

### 2.3 `POST /bridge/{slug}`

Invokes a named operation.

**Request body:**
```json
{ "payload": { /* operation-specific fields */ } }
```
- `payload` is required and must be a JSON object.
- The contents of `payload` must validate against the `request_schema` published for `slug` in `/discovery`.

**200 response - shape:**
```json
{
  "compatibility_level": "v1",
  "payload": { /* operation-specific result */ }
}
```
The `payload` object must validate against the operation's published `response_schema`.

**200 response - sample for `POST /bridge/check-utility-customer`** (applicant matched a record):
```json
{
  "compatibility_level": "v1",
  "payload": { "eligible": true }
}
```

**200 response - sample for `POST /bridge/check-utility-customer`** (no matching record):
```json
{
  "compatibility_level": "v1",
  "payload": { "eligible": false }
}
```

**Error responses** are documented in §4.

---

## 3. JSON Schema Requirements

The `request_schema` and `response_schema` returned from `/discovery` are the single source of truth for operation shape. [JSON Schema](https://json-schema.org/) is used to validate the request and response in both the bridge and CiviForm container the expected data. They must satisfy **all** of the following.

### 3.1 Dialect
- Must use **JSON Schema draft 2020-12**.
- Each schema document **must** declare:
  ```json
  "$schema": "https://json-schema.org/draft/2020-12/schema"
  ```

### 3.2 Identity and documentation
Each schema document **must** include:
- `$id` - an absolute URI that uniquely identifies this schema (e.g. `https://your-org.example/schemas/<slug>-request.json`, `https://your-org.example/schemas/<slug>-response.json`). Stable across deploys. The URL does not have to actually exist.
- `title` - short human-readable name.
- `description` - one or two sentences explaining what the schema describes.
- `type: "object"` at the root.

### 3.3 Properties
Each property in `properties` **must** include:
- `type` - primitive JSON Schema type (`string`, `boolean`, `integer`, `number`, `array`, `object`).
- `title` - short human-readable label (used by CiviForm UI).
- `description` - what the field means and any formatting expectations (e.g. "Two-letter state code").

Use snake_case for property names.

### 3.4 Strictness
Each schema **must** set:
- `required` - list every field the caller must send (request) or every field the response will always contain.
- `additionalProperties: false` - reject unknown fields. This is what makes the contract discoverable and prevents silent drift between client and server.

### 3.5 Example
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://your-org.example/schemas/check-customer-request.json",
  "title": "Check Customer Request",
  "description": "Identity and address fields used to look up a customer record.",
  "type": "object",
  "properties": {
    "first_name": {
      "type": "string",
      "title": "First Name",
      "description": "Applicant's legal first name."
    },
    "account_number": {
      "type": "string",
      "title": "Account Number",
      "description": "Account number as printed on a recent bill."
    }
  },
  "required": ["first_name", "account_number"],
  "additionalProperties": false
}
```

### 3.6 Schema/handler parity
The schema is contract; the handler must enforce it. Whatever you publish in `request_schema` is what your bridge handler must validate against, and whatever you publish in `response_schema` is what your handler must produce. If a field is not in the schema, it must not appear in the wire payload.

---

## 4. Error Handling (RFC 9457 Problem Details)

All non-2xx responses **must** be [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457.html) documents.

- `Content-Type: application/problem+json`
- HTTP status code on the response matches the `status` field in the body.

### 4.1 Standard problem body
```json
{
  "type": "about:blank",
  "title": "Short summary",
  "status": 400,
  "detail": "Human-readable explanation of this specific occurrence."
}
```
Required field: `type`. Use `"about:blank"` unless you publish a richer problem-type registry.

### 4.2 Validation errors (HTTP 422)
When request payload validation fails, return **422 Unprocessable Entity** with an extended problem body:

```json
{
  "type": "about:blank",
  "title": "Validation Error",
  "status": 422,
  "detail": "Request payload failed validation",
  "validation_errors": [
    { "name": "first_name", "message": "first_name is required" },
    { "name": "zip",        "message": "zip must be a string"   }
  ]
}
```
- `validation_errors` is an array; each entry has required `name` and `message` strings.
- `name` should match the offending property name from the request schema.
- Return **all** validation failures in one response, not just the first.

### 4.3 Status code summary

| Status | When to use |
|--------|-------------|
| 400 Bad Request          | Body is missing, not JSON, or is JSON but lacks the top-level `payload` object. |
| 401 Unauthorized         | Authentication required and missing/invalid (if your deployment uses auth). |
| 404 Not Found            | The `{slug}` in `/bridge/{slug}` does not match any operation in `/discovery`. |
| 422 Unprocessable Entity | Body is well-formed JSON with a `payload` object, but the payload fails schema validation. |
| 500 Internal Server Error| Unexpected server-side failure. Include a generic `detail`; do not leak stack traces. |

### 4.4 Distinguishing 400 vs 422
- **400** = the *envelope* is wrong (not JSON, no `payload` key).
- **422** = the envelope is right, but the *contents* of `payload` violate the operation's schema.

---

## 5. Operation Lifecycle

The recommended request flow for a new operation:

1. **Identify the slug.** Pick a stable, kebab-case name (e.g. `check-utility-customer`). This goes in `/discovery` and in the URL path.
2. **Define the JSON Schemas.** Author `request_schema` and `response_schema` per §3. Treat them as the source of truth.
3. **Implement the handler.** On `POST /bridge/{slug}`:
   1. Parse the JSON body. If parsing fails or `payload` is missing, return **400**.
   2. Validate `payload` against `request_schema`. If invalid, return **422** with `validation_errors`.
   3. Execute the business logic.
   4. Build the response payload so it validates against `response_schema`.
   5. Wrap it in `{ "compatibility_level": "v1", "payload": ... }` and return **200**.
4. **Register the operation in `/discovery`.** Add an entry under `endpoints` with the slug as the key and the schemas inline.
5. **Test.** At minimum: happy path, no-match path, missing-required-field path (expect 422), unknown-slug path (expect 404), non-JSON body (expect 400).

---

## 6. Versioning and Compatibility

- **Additive changes** (new optional response fields, new operations, longer descriptions) do not require a new `compatibility_level`. Add the field to the schema, leave it out of `required`, and document it.
- **Breaking changes** (renaming a field, changing a type, making an optional field required, removing a field) require a new `compatibility_level`. Continue to serve `v1` while clients migrate, ideally under a new slug or a new endpoint entry.
- Never reuse a slug for a different operation. Slugs are durable.
- `$id` URIs should change when the schema changes in a breaking way; minor additive updates can keep the same `$id`.

---

## 7. Operational Notes

- **Idempotency**: bridge operations should be safe to retry.
- **Logging**: log the slug, HTTP status, and a correlation ID per request. Do not log raw payloads if they may contain PII.
- **Performance**: `/discovery` is fetched rarely; prioritize correctness over throughput. `/bridge/{slug}` should be fast - CiviForm calls it inline during form submission.

---

## 8. Quick Checklist

Before shipping a bridge service, verify:

- [ ] `GET /health-check` returns 200 with an integer `timestamp`.
- [ ] `GET /discovery` returns 200 with an `endpoints` object keyed by slug.
- [ ] Every endpoint entry has `compatibility_level`, `description`, `uri`, `request_schema`, `response_schema`.
- [ ] Every schema declares `$schema`, `$id`, `title`, `description`, `type: "object"`.
- [ ] Every schema lists its `required` fields and sets `additionalProperties: false`.
- [ ] Every property has `type`, `title`, and `description`.
- [ ] Successful bridge responses are `{ "compatibility_level": "...", "payload": {...} }`.
- [ ] All errors are `application/problem+json` with `type`, `title`, `status`, `detail`.
- [ ] Validation failures return 422 with a `validation_errors` array (`name` + `message` per entry).
- [ ] Unknown slugs return 404; malformed envelopes return 400.

---

## 9. Samples

There are currently samples in `./python`, `./node`, and `./dotnet`.

These samples are quickly put together to demonstrate the concepts and not drop in production ready templates.
