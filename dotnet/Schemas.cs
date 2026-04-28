// <summary>
// JSON Schema definitions and discovery document for the bridge endpoints.
//
// Defines the request/response schemas for each bridge slug and a
// <see cref="Schemas.DiscoveryDocument"/> builder that advertises them to
// clients.
// </summary>

using System.Text.Json.Nodes;

namespace Bridge;

public static class Schemas
{
    public const string CompatibilityLevel = "v1";
    public const string BridgeSlug = "check-utility-customer";
    public const string LookupBridgeSlug = "lookup-utility-customer";

    private const string JsonSchemaDraft = "https://json-schema.org/draft/2020-12/schema";
    private const string SchemaIdBase = "https://civiform.us/schemas";

    private static JsonObject StringField(string title, string description) => new()
    {
        ["type"] = "string",
        ["pattern"] = "\\S",
        ["title"] = title,
        ["description"] = description,
    };

    public static JsonObject BuildRequestSchema() => new()
    {
        ["$schema"] = JsonSchemaDraft,
        ["$id"] = $"{SchemaIdBase}/check-utility-customer-request.json",
        ["title"] = "Check Utility Customer Request",
        ["description"] = "Applicant-supplied identity and address fields used to look up a utility customer record.",
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["first_name"] = StringField("First Name", "Applicant's legal first name."),
            ["last_name"] = StringField("Last Name", "Applicant's legal last name."),
            ["account_number"] = StringField("Account Number", "Utility account number as printed on a recent bill."),
            ["address1"] = StringField("Address Line 1", "Street address of the service location."),
            ["city"] = StringField("City", "City of the service location."),
            ["state"] = StringField("State", "Two-letter state code of the service location."),
            ["zip"] = StringField("ZIP Code", "Postal ZIP code of the service location."),
        },
        ["required"] = new JsonArray("first_name", "last_name", "account_number", "address1", "city", "state", "zip"),
        ["additionalProperties"] = false,
    };

    public static JsonObject BuildResponseSchema() => new()
    {
        ["$schema"] = JsonSchemaDraft,
        ["$id"] = $"{SchemaIdBase}/check-utility-customer-response.json",
        ["title"] = "Check Utility Customer Response",
        ["description"] = "Eligibility decision for a utility customer lookup.",
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["eligible"] = new JsonObject
            {
                ["type"] = "boolean",
                ["title"] = "Eligible",
                ["description"] = "True when the applicant matches a utility customer record.",
            },
        },
        ["required"] = new JsonArray("eligible"),
        ["additionalProperties"] = false,
    };

    public static JsonObject BuildLookupRequestSchema() => new()
    {
        ["$schema"] = JsonSchemaDraft,
        ["$id"] = $"{SchemaIdBase}/lookup-utility-customer-request.json",
        ["title"] = "Lookup Utility Customer Request",
        ["description"] = "Customer identifier used to confirm that a utility customer record exists.",
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["customer_id"] = StringField("Customer ID", "Internal identifier for the customer record to look up."),
        },
        ["required"] = new JsonArray("customer_id"),
        ["additionalProperties"] = false,
    };

    public static JsonObject BuildLookupResponseSchema() => new()
    {
        ["$schema"] = JsonSchemaDraft,
        ["$id"] = $"{SchemaIdBase}/lookup-utility-customer-response.json",
        ["title"] = "Lookup Utility Customer Response",
        ["description"] = "Whether a utility customer record exists for the given customer identifier.",
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["found"] = new JsonObject
            {
                ["type"] = "boolean",
                ["title"] = "Found",
                ["description"] = "True when a utility customer record exists for the supplied customer_id.",
            },
        },
        ["required"] = new JsonArray("found"),
        ["additionalProperties"] = false,
    };

    /// <summary>
    /// Return the discovery payload describing every bridge endpoint.
    ///
    /// Each entry advertises its URI, compatibility level, human-readable
    /// description, and the JSON Schemas for its request and response
    /// bodies.
    /// </summary>
    public static JsonObject DiscoveryDocument() => new()
    {
        ["endpoints"] = new JsonObject
        {
            [$"/bridge/{BridgeSlug}"] = new JsonObject
            {
                ["compatibility_level"] = CompatibilityLevel,
                ["description"] = "Check whether an applicant is an existing utility customer by name, account number, and address.",
                ["uri"] = $"/bridge/{BridgeSlug}",
                ["request_schema"] = BuildRequestSchema(),
                ["response_schema"] = BuildResponseSchema(),
            },
            [$"/bridge/{LookupBridgeSlug}"] = new JsonObject
            {
                ["compatibility_level"] = CompatibilityLevel,
                ["description"] = "Look up whether a utility customer exists by customer_id.",
                ["uri"] = $"/bridge/{LookupBridgeSlug}",
                ["request_schema"] = BuildLookupRequestSchema(),
                ["response_schema"] = BuildLookupResponseSchema(),
            },
        },
    };
}
