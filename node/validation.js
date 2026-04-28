/**
 * @file JSON Schema validation for bridge request and response payloads.
 */

"use strict";

const Ajv = require("ajv/dist/2020");

const {
  BRIDGE_SLUG,
  LOOKUP_BRIDGE_SLUG,
  REQUEST_SCHEMA,
  RESPONSE_SCHEMA,
  LOOKUP_REQUEST_SCHEMA,
  LOOKUP_RESPONSE_SCHEMA,
} = require("./schemas");

const ajv = new Ajv({ allErrors: true, strict: false });

const REQUEST_VALIDATORS = {
  [BRIDGE_SLUG]: ajv.compile(REQUEST_SCHEMA),
  [LOOKUP_BRIDGE_SLUG]: ajv.compile(LOOKUP_REQUEST_SCHEMA),
};

const RESPONSE_VALIDATORS = {
  [BRIDGE_SLUG]: ajv.compile(RESPONSE_SCHEMA),
  [LOOKUP_BRIDGE_SLUG]: ajv.compile(LOOKUP_RESPONSE_SCHEMA),
};

/**
 * Derive the offending field name from an Ajv error object.
 *
 * Falls back to `params.missingProperty` for `required` errors (which
 * have no `instancePath`), to `params.additionalProperty` for
 * `additionalProperties` errors, and to `"payload"` when no field can
 * be identified.
 */
function fieldName(error) {
  if (error.instancePath) {
    return error.instancePath.replace(/^\//, "").split("/")[0];
  }
  if (error.keyword === "required") {
    return error.params.missingProperty;
  }
  if (error.keyword === "additionalProperties") {
    return error.params.additionalProperty;
  }
  return "payload";
}

function errorMessage(error) {
  if (error.keyword === "additionalProperties") {
    return `additional property '${error.params.additionalProperty}' is not allowed`;
  }
  return error.message;
}

/** Run `validator` over `payload` and return `{name, message}` objects. */
function formatErrors(validator, payload) {
  const valid = validator(payload);
  if (valid) return [];
  return (validator.errors || []).map((e) => ({
    name: fieldName(e),
    message: errorMessage(e),
  }));
}

/**
 * Validate a request `payload` against the schema for `slug`.
 *
 * Returns a list of `{name, message}` error objects, empty on success.
 */
function validateRequest(slug, payload) {
  return formatErrors(REQUEST_VALIDATORS[slug], payload);
}

/**
 * Validate a response `payload` against the schema for `slug`.
 *
 * Returns a list of `{name, message}` error objects, empty on success.
 */
function validateResponse(slug, payload) {
  return formatErrors(RESPONSE_VALIDATORS[slug], payload);
}

module.exports = { validateRequest, validateResponse };
