/**
 * @file Express response helpers for success and RFC 7807 problem responses.
 */

"use strict";

const { COMPATIBILITY_LEVEL } = require("./schemas");

const PROBLEM_CONTENT_TYPE = "application/problem+json";

/**
 * Send an RFC 7807 `application/problem+json` response.
 *
 * @param {import("express").Response} res - Express response to write to.
 * @param {number} status - HTTP status code to set on the response.
 * @param {string} title - Short, human-readable summary of the problem type.
 * @param {string} detail - Human-readable explanation specific to this occurrence.
 * @param {object} [extra] - Optional object merged into the problem body
 *   (e.g. to attach `validation_errors`).
 */
function problem(res, status, title, detail, extra) {
  const body = {
    type: "about:blank",
    title,
    status,
    detail,
    ...(extra || {}),
  };
  res.status(status).type(PROBLEM_CONTENT_TYPE).send(JSON.stringify(body));
}

/**
 * Wrap `payload` in the standard success envelope and send it as JSON.
 *
 * The envelope includes the bridge `compatibility_level` so clients
 * can detect contract changes.
 */
function ok(res, payload) {
  res.json({
    compatibility_level: COMPATIBILITY_LEVEL,
    payload,
  });
}

module.exports = { problem, ok, PROBLEM_CONTENT_TYPE };
