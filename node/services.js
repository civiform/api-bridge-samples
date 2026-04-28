/**
 * @file Bridge request handlers.
 *
 * Each bridge slug routes to a handler that validates the request payload,
 * performs a customer lookup, validates the response payload, and returns
 * either an `ok` envelope or a problem response.
 */

"use strict";

const { findCustomer, findCustomerById } = require("./data");
const { ok, problem } = require("./responses");
const { BRIDGE_SLUG, LOOKUP_BRIDGE_SLUG } = require("./schemas");
const { validateRequest, validateResponse } = require("./validation");

/** Return a 422 problem response listing request `errors`. */
function validationError(res, errors) {
  problem(
    res,
    422,
    "Validation Error",
    "Request payload failed validation",
    { validation_errors: errors },
  );
}

/**
 * Validate `payload` against `slug`'s response schema and return it.
 *
 * On schema failure, returns a 500 problem response so the
 * contract violation is surfaced rather than silently shipped.
 */
function respond(res, slug, payload) {
  const errors = validateResponse(slug, payload);
  if (errors.length) {
    return problem(
      res,
      500,
      "Internal Server Error",
      "Response payload failed validation",
      { validation_errors: errors },
    );
  }
  return ok(res, payload);
}

/**
 * Handle the `check-utility-customer` bridge.
 *
 * Validates the request, looks up a customer by name/account/address,
 * and returns `{eligible: boolean}`.
 */
function checkCustomer(res, slug, payload) {
  const errors = validateRequest(slug, payload);
  if (errors.length) return validationError(res, errors);

  const match = findCustomer(
    payload.first_name,
    payload.last_name,
    payload.account_number,
    payload.address1,
    payload.city,
    payload.state,
    payload.zip,
  );
  return respond(res, slug, { eligible: match !== null });
}

/**
 * Handle the `lookup-utility-customer` bridge.
 *
 * Validates the request, looks up a customer by `customer_id`, and
 * returns `{found: boolean}`.
 */
function lookupCustomer(res, slug, payload) {
  const errors = validateRequest(slug, payload);
  if (errors.length) return validationError(res, errors);

  const match = findCustomerById(payload.customer_id);
  return respond(res, slug, { found: match !== null });
}

const HANDLERS = {
  [BRIDGE_SLUG]: checkCustomer,
  [LOOKUP_BRIDGE_SLUG]: lookupCustomer,
};

/**
 * Route a bridge request body to the handler registered for `slug`.
 *
 * Returns 404 when `slug` is unknown, 400 when `body` is not a JSON
 * object containing a `payload` key, and otherwise the handler's
 * response.
 */
function handleBridgeRequest(res, slug, body) {
  const handler = HANDLERS[slug];
  if (!handler) {
    return problem(res, 404, "Not Found", `Unknown bridge endpoint: ${slug}`);
  }

  if (
    !body ||
    typeof body !== "object" ||
    Array.isArray(body) ||
    !Object.prototype.hasOwnProperty.call(body, "payload")
  ) {
    return problem(
      res,
      400,
      "Bad Request",
      "Request body must be JSON with a 'payload' object",
    );
  }

  return handler(res, slug, body.payload);
}

module.exports = { handleBridgeRequest };
