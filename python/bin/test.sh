#!/usr/bin/env bash
set -uo pipefail

BASE_URL="${BASE_URL:-http://localhost:5010}"
BODY_FILE="$(mktemp)"
trap 'rm -f "$BODY_FILE"' EXIT

if ! command -v jq >/dev/null 2>&1; then
    echo "ERROR: jq is required to run this test script." >&2
    exit 2
fi

pass_count=0
fail_count=0

assert() {
    local label="$1"
    local actual="$2"
    local expected="$3"
    if [ "$actual" = "$expected" ]; then
        echo "  PASS: $label ($actual)"
        pass_count=$((pass_count + 1))
    else
        echo "  FAIL: $label — expected [$expected], got [$actual]"
        fail_count=$((fail_count + 1))
    fi
}

http_request() {
    local method="$1"
    local path="$2"
    shift 2
    curl -s -o "$BODY_FILE" -w "%{http_code}" -X "$method" "$BASE_URL$path" "$@"
}

echo
echo "Test 1: GET /health-check returns 200 + timestamp"
code=$(http_request GET /health-check)
assert "status code" "$code" "200"
assert "timestamp is number" "$(jq -r '."timestamp" | type' "$BODY_FILE")" "number"

echo
echo "Test 2: GET /discovery returns 200 + describes bridge slugs"
code=$(http_request GET /discovery)
assert "status code" "$code" "200"
assert "endpoint compatibility_level" \
    "$(jq -r '.endpoints["/bridge/check-utility-customer"].compatibility_level' "$BODY_FILE")" "v1"
assert "endpoint uri" \
    "$(jq -r '.endpoints["/bridge/check-utility-customer"].uri' "$BODY_FILE")" "/bridge/check-utility-customer"
assert "lookup endpoint compatibility_level" \
    "$(jq -r '.endpoints["/bridge/lookup-utility-customer"].compatibility_level' "$BODY_FILE")" "v1"
assert "lookup endpoint uri" \
    "$(jq -r '.endpoints["/bridge/lookup-utility-customer"].uri' "$BODY_FILE")" "/bridge/lookup-utility-customer"
assert "request_schema is object" \
    "$(jq -r '.endpoints["/bridge/check-utility-customer"].request_schema | type' "$BODY_FILE")" "object"
assert "response_schema is object" \
    "$(jq -r '.endpoints["/bridge/check-utility-customer"].response_schema | type' "$BODY_FILE")" "object"
assert "request_schema has JSON Schema draft" \
    "$(jq -r '.endpoints["/bridge/check-utility-customer"].request_schema."$schema"' "$BODY_FILE")" \
    "https://json-schema.org/draft/2020-12/schema"
assert "response_schema has additionalProperties false" \
    "$(jq -r '.endpoints["/bridge/check-utility-customer"].response_schema.additionalProperties' "$BODY_FILE")" "false"
assert "lookup request_schema is object" \
    "$(jq -r '.endpoints["/bridge/lookup-utility-customer"].request_schema | type' "$BODY_FILE")" "object"
assert "lookup response_schema has JSON Schema draft" \
    "$(jq -r '.endpoints["/bridge/lookup-utility-customer"].response_schema."$schema"' "$BODY_FILE")" \
    "https://json-schema.org/draft/2020-12/schema"

echo
echo "Test 3: POST /bridge/check-utility-customer with a matching record → eligible"
code=$(http_request POST /bridge/check-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":{"first_name":"Ada","last_name":"Lovelace","account_number":"UA-8821-4417","address1":"123 Analytical Way","city":"Springfield","state":"IL","zip":"62704"}}')
assert "status code" "$code" "200"
assert "compatibility_level" "$(jq -r '.compatibility_level' "$BODY_FILE")" "v1"
assert "eligible" "$(jq -r '.payload.eligible' "$BODY_FILE")" "true"
assert "no customer_id leaked" "$(jq -r '.payload | has("customer_id")' "$BODY_FILE")" "false"

echo
echo "Test 4: POST /bridge/check-utility-customer with wrong account → not eligible"
code=$(http_request POST /bridge/check-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":{"first_name":"Ada","last_name":"Lovelace","account_number":"UA-0000-0000","address1":"123 Analytical Way","city":"Springfield","state":"IL","zip":"62704"}}')
assert "status code" "$code" "200"
assert "eligible" "$(jq -r '.payload.eligible' "$BODY_FILE")" "false"

echo
echo "Test 5: POST missing address fields → 422 Validation Error"
code=$(http_request POST /bridge/check-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":{"first_name":"Ada","last_name":"Lovelace","account_number":"UA-8821-4417"}}')
assert "status code" "$code" "422"
assert "title" "$(jq -r '.title' "$BODY_FILE")" "Validation Error"
assert "has validation_errors" \
    "$(jq -r '.validation_errors | length > 0' "$BODY_FILE")" "true"
assert "error names address1" \
    "$(jq -r '[.validation_errors[].name] | index("address1") != null' "$BODY_FILE")" "true"
assert "error names city" \
    "$(jq -r '[.validation_errors[].name] | index("city") != null' "$BODY_FILE")" "true"
assert "error names state" \
    "$(jq -r '[.validation_errors[].name] | index("state") != null' "$BODY_FILE")" "true"
assert "error names zip" \
    "$(jq -r '[.validation_errors[].name] | index("zip") != null' "$BODY_FILE")" "true"

echo
echo "Test 6: POST /bridge/unknown-slug → 404"
code=$(http_request POST /bridge/unknown-slug \
    -H 'Content-Type: application/json' \
    -d '{"payload":{"first_name":"x","last_name":"x","account_number":"y","address1":"z","city":"z","state":"z","zip":"z"}}')
assert "status code" "$code" "404"
assert "title" "$(jq -r '.title' "$BODY_FILE")" "Not Found"

echo
echo "Test 7: POST non-JSON body → 400"
code=$(http_request POST /bridge/check-utility-customer \
    -H 'Content-Type: text/plain' \
    -d 'not json')
assert "status code" "$code" "400"
assert "title" "$(jq -r '.title' "$BODY_FILE")" "Bad Request"

echo
echo "Test 8: POST /bridge/lookup-utility-customer with known id → found true"
code=$(http_request POST /bridge/lookup-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":{"customer_id":"C-10001"}}')
assert "status code" "$code" "200"
assert "compatibility_level" "$(jq -r '.compatibility_level' "$BODY_FILE")" "v1"
assert "found" "$(jq -r '.payload.found' "$BODY_FILE")" "true"

echo
echo "Test 9: POST /bridge/lookup-utility-customer with unknown id → found false"
code=$(http_request POST /bridge/lookup-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":{"customer_id":"C-99999"}}')
assert "status code" "$code" "200"
assert "found" "$(jq -r '.payload.found' "$BODY_FILE")" "false"

echo
echo "Test 10: POST /bridge/lookup-utility-customer missing customer_id → 422"
code=$(http_request POST /bridge/lookup-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":{}}')
assert "status code" "$code" "422"
assert "title" "$(jq -r '.title' "$BODY_FILE")" "Validation Error"
assert "error names customer_id" \
    "$(jq -r '[.validation_errors[].name] | index("customer_id") != null' "$BODY_FILE")" "true"

echo
echo "Test 11: POST blank string field → 422 with field name in errors"
code=$(http_request POST /bridge/check-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":{"first_name":"   ","last_name":"Lovelace","account_number":"UA-8821-4417","address1":"123 Analytical Way","city":"Springfield","state":"IL","zip":"62704"}}')
assert "status code" "$code" "422"
assert "title" "$(jq -r '.title' "$BODY_FILE")" "Validation Error"
assert "error names first_name" \
    "$(jq -r '[.validation_errors[].name] | index("first_name") != null' "$BODY_FILE")" "true"

echo
echo "Test 12: POST wrong field type → 422 with field name in errors"
code=$(http_request POST /bridge/check-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":{"first_name":123,"last_name":"Lovelace","account_number":"UA-8821-4417","address1":"123 Analytical Way","city":"Springfield","state":"IL","zip":"62704"}}')
assert "status code" "$code" "422"
assert "title" "$(jq -r '.title' "$BODY_FILE")" "Validation Error"
assert "error names first_name" \
    "$(jq -r '[.validation_errors[].name] | index("first_name") != null' "$BODY_FILE")" "true"

echo
echo "Test 13: POST unexpected extra field → 422 with extra field cited"
code=$(http_request POST /bridge/check-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":{"first_name":"Ada","last_name":"Lovelace","account_number":"UA-8821-4417","address1":"123 Analytical Way","city":"Springfield","state":"IL","zip":"62704","ssn":"123-45-6789"}}')
assert "status code" "$code" "422"
assert "title" "$(jq -r '.title' "$BODY_FILE")" "Validation Error"
assert "error mentions ssn" \
    "$(jq -r '[.validation_errors[].message] | map(test("ssn")) | any' "$BODY_FILE")" "true"

echo
echo "Test 14: POST non-object payload → 422"
code=$(http_request POST /bridge/check-utility-customer \
    -H 'Content-Type: application/json' \
    -d '{"payload":[1,2,3]}')
assert "status code" "$code" "422"
assert "title" "$(jq -r '.title' "$BODY_FILE")" "Validation Error"
assert "has validation_errors" \
    "$(jq -r '.validation_errors | length > 0' "$BODY_FILE")" "true"

echo
echo "-----"
echo "Results: $pass_count passed, $fail_count failed"
[ "$fail_count" -eq 0 ]
