/**
 * @file JSON Schema definitions and discovery document for the bridge endpoints.
 *
 * Defines the request/response schemas for each bridge slug and a
 * `discoveryDocument` builder that advertises them to clients.
 */

"use strict";

const COMPATIBILITY_LEVEL = "v1";
const BRIDGE_SLUG = "check-utility-customer";
const LOOKUP_BRIDGE_SLUG = "lookup-utility-customer";

const JSON_SCHEMA_DRAFT = "https://json-schema.org/draft/2020-12/schema";
const SCHEMA_ID_BASE = "https://civiform.us/schemas";

const REQUEST_SCHEMA = {
  $schema: JSON_SCHEMA_DRAFT,
  $id: `${SCHEMA_ID_BASE}/check-utility-customer-request.json`,
  title: "Check Utility Customer Request",
  description:
    "Applicant-supplied identity and address fields used to look up a utility customer record.",
  type: "object",
  properties: {
    first_name: {
      type: "string",
      pattern: "\\S",
      title: "First Name",
      description: "Applicant's legal first name.",
    },
    last_name: {
      type: "string",
      pattern: "\\S",
      title: "Last Name",
      description: "Applicant's legal last name.",
    },
    account_number: {
      type: "string",
      pattern: "\\S",
      title: "Account Number",
      description: "Utility account number as printed on a recent bill.",
    },
    address1: {
      type: "string",
      pattern: "\\S",
      title: "Address Line 1",
      description: "Street address of the service location.",
    },
    city: {
      type: "string",
      pattern: "\\S",
      title: "City",
      description: "City of the service location.",
    },
    state: {
      type: "string",
      pattern: "\\S",
      title: "State",
      description: "Two-letter state code of the service location.",
    },
    zip: {
      type: "string",
      pattern: "\\S",
      title: "ZIP Code",
      description: "Postal ZIP code of the service location.",
    },
  },
  required: [
    "first_name",
    "last_name",
    "account_number",
    "address1",
    "city",
    "state",
    "zip",
  ],
  additionalProperties: false,
};

const RESPONSE_SCHEMA = {
  $schema: JSON_SCHEMA_DRAFT,
  $id: `${SCHEMA_ID_BASE}/check-utility-customer-response.json`,
  title: "Check Utility Customer Response",
  description: "Eligibility decision for a utility customer lookup.",
  type: "object",
  properties: {
    eligible: {
      type: "boolean",
      title: "Eligible",
      description:
        "True when the applicant matches a utility customer record.",
    },
  },
  required: ["eligible"],
  additionalProperties: false,
};

const LOOKUP_REQUEST_SCHEMA = {
  $schema: JSON_SCHEMA_DRAFT,
  $id: `${SCHEMA_ID_BASE}/lookup-utility-customer-request.json`,
  title: "Lookup Utility Customer Request",
  description:
    "Customer identifier used to confirm that a utility customer record exists.",
  type: "object",
  properties: {
    customer_id: {
      type: "string",
      pattern: "\\S",
      title: "Customer ID",
      description: "Internal identifier for the customer record to look up.",
    },
  },
  required: ["customer_id"],
  additionalProperties: false,
};

const LOOKUP_RESPONSE_SCHEMA = {
  $schema: JSON_SCHEMA_DRAFT,
  $id: `${SCHEMA_ID_BASE}/lookup-utility-customer-response.json`,
  title: "Lookup Utility Customer Response",
  description:
    "Whether a utility customer record exists for the given customer identifier.",
  type: "object",
  properties: {
    found: {
      type: "boolean",
      title: "Found",
      description:
        "True when a utility customer record exists for the supplied customer_id.",
    },
  },
  required: ["found"],
  additionalProperties: false,
};

/**
 * Return the discovery payload describing every bridge endpoint.
 *
 * Each entry advertises its URI, compatibility level, human-readable
 * description, and the JSON Schemas for its request and response
 * bodies.
 */
function discoveryDocument() {
  return {
    endpoints: {
      [`/bridge/${BRIDGE_SLUG}`]: {
        compatibility_level: COMPATIBILITY_LEVEL,
        description:
          "Check whether an applicant is an existing utility customer by name, account number, and address.",
        uri: `/bridge/${BRIDGE_SLUG}`,
        request_schema: REQUEST_SCHEMA,
        response_schema: RESPONSE_SCHEMA,
      },
      [`/bridge/${LOOKUP_BRIDGE_SLUG}`]: {
        compatibility_level: COMPATIBILITY_LEVEL,
        description:
          "Look up whether a utility customer exists by customer_id.",
        uri: `/bridge/${LOOKUP_BRIDGE_SLUG}`,
        request_schema: LOOKUP_REQUEST_SCHEMA,
        response_schema: LOOKUP_RESPONSE_SCHEMA,
      },
    },
  };
}

module.exports = {
  COMPATIBILITY_LEVEL,
  BRIDGE_SLUG,
  LOOKUP_BRIDGE_SLUG,
  REQUEST_SCHEMA,
  RESPONSE_SCHEMA,
  LOOKUP_REQUEST_SCHEMA,
  LOOKUP_RESPONSE_SCHEMA,
  discoveryDocument,
};
