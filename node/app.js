/**
 * @file Express application entry point exposing the bridge HTTP endpoints.
 */

"use strict";

const express = require("express");

const { discoveryDocument } = require("./schemas");
const { handleBridgeRequest } = require("./services");

const app = express();

app.use(express.json());

/** Return the current server time as a liveness signal. */
app.get("/health-check", (req, res) => {
  res.json({ timestamp: Math.floor(Date.now() / 1000) });
});

/** Return the bridge discovery document describing every endpoint. */
app.get("/discovery", (req, res) => {
  res.json(discoveryDocument());
});

/** Dispatch a bridge request identified by `slug` to its handler. */
app.post("/bridge/:slug", (req, res) => {
  handleBridgeRequest(res, req.params.slug, req.body);
});

const PORT = Number(process.env.PORT || 5011);

if (require.main === module) {
  app.listen(PORT, "0.0.0.0", () => {
    console.log(`Bridge service listening on http://0.0.0.0:${PORT}`);
  });
}

module.exports = app;
