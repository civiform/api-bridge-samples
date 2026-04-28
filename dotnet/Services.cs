// <summary>
// Bridge request handlers.
//
// Each bridge slug routes to a handler that validates the request payload,
// performs a customer lookup, validates the response payload, and returns
// either an <c>ok</c> envelope or a problem response.
// </summary>

using System.Text.Json.Nodes;

namespace Bridge;

public static class Services
{
    /// <summary>Return a 422 problem response listing request <paramref name="errors"/>.</summary>
    private static IResult ValidationError(List<ValidationError> errors)
    {
        var array = new JsonArray();
        foreach (var error in errors)
        {
            array.Add(new JsonObject
            {
                ["name"] = error.Name,
                ["message"] = error.Message,
            });
        }
        return Responses.Problem(
            422,
            "Validation Error",
            "Request payload failed validation",
            new JsonObject { ["validation_errors"] = array });
    }

    /// <summary>
    /// Validate <paramref name="payload"/> against <paramref name="slug"/>'s
    /// response schema and return it.
    ///
    /// On schema failure, returns a 500 problem response so the contract
    /// violation is surfaced rather than silently shipped.
    /// </summary>
    private static IResult Respond(string slug, JsonNode payload)
    {
        var errors = Validation.ValidateResponse(slug, payload);
        if (errors.Count > 0)
        {
            var array = new JsonArray();
            foreach (var error in errors)
            {
                array.Add(new JsonObject
                {
                    ["name"] = error.Name,
                    ["message"] = error.Message,
                });
            }
            return Responses.Problem(
                500,
                "Internal Server Error",
                "Response payload failed validation",
                new JsonObject { ["validation_errors"] = array });
        }
        return Responses.Ok(payload);
    }

    /// <summary>
    /// Handle the <c>check-utility-customer</c> bridge.
    ///
    /// Validates the request, looks up a customer by name/account/address,
    /// and returns <c>{eligible: bool}</c>.
    /// </summary>
    private static IResult CheckCustomer(string slug, JsonNode? payload)
    {
        var errors = Validation.ValidateRequest(slug, payload);
        if (errors.Count > 0) return ValidationError(errors);

        var obj = payload!.AsObject();
        var match = Data.FindCustomer(
            (string)obj["first_name"]!,
            (string)obj["last_name"]!,
            (string)obj["account_number"]!,
            (string)obj["address1"]!,
            (string)obj["city"]!,
            (string)obj["state"]!,
            (string)obj["zip"]!);

        return Respond(slug, new JsonObject { ["eligible"] = match is not null });
    }

    /// <summary>
    /// Handle the <c>lookup-utility-customer</c> bridge.
    ///
    /// Validates the request, looks up a customer by <c>customer_id</c>, and
    /// returns <c>{found: bool}</c>.
    /// </summary>
    private static IResult LookupCustomer(string slug, JsonNode? payload)
    {
        var errors = Validation.ValidateRequest(slug, payload);
        if (errors.Count > 0) return ValidationError(errors);

        var obj = payload!.AsObject();
        var match = Data.FindCustomerById((string)obj["customer_id"]!);
        return Respond(slug, new JsonObject { ["found"] = match is not null });
    }

    private static readonly Dictionary<string, Func<string, JsonNode?, IResult>> Handlers = new()
    {
        [Schemas.BridgeSlug] = CheckCustomer,
        [Schemas.LookupBridgeSlug] = LookupCustomer,
    };

    /// <summary>
    /// Route a bridge request body to the handler registered for
    /// <paramref name="slug"/>.
    ///
    /// Returns 404 when <paramref name="slug"/> is unknown, 400 when
    /// <paramref name="body"/> is not a JSON object containing a
    /// <c>payload</c> key, and otherwise the handler's response.
    /// </summary>
    public static IResult HandleBridgeRequest(string slug, JsonNode? body)
    {
        if (!Handlers.TryGetValue(slug, out var handler))
        {
            return Responses.Problem(404, "Not Found", $"Unknown bridge endpoint: {slug}");
        }

        if (body is not JsonObject obj || !obj.ContainsKey("payload"))
        {
            return Responses.Problem(
                400,
                "Bad Request",
                "Request body must be JSON with a 'payload' object");
        }

        return handler(slug, obj["payload"]);
    }
}
