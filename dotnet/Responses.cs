// <summary>
// ASP.NET Core response helpers for success and RFC 7807 problem responses.
// </summary>

using System.Text.Json.Nodes;

namespace Bridge;

public static class Responses
{
    public const string ProblemContentType = "application/problem+json";

    /// <summary>
    /// Build an RFC 7807 <c>application/problem+json</c> result.
    /// </summary>
    /// <param name="status">HTTP status code to set on the response.</param>
    /// <param name="title">Short, human-readable summary of the problem type.</param>
    /// <param name="detail">Human-readable explanation specific to this occurrence.</param>
    /// <param name="extra">Optional object merged into the problem body
    /// (e.g. to attach <c>validation_errors</c>).</param>
    public static IResult Problem(int status, string title, string detail, JsonObject? extra = null)
    {
        var body = new JsonObject
        {
            ["type"] = "about:blank",
            ["title"] = title,
            ["status"] = status,
            ["detail"] = detail,
        };
        if (extra is not null)
        {
            foreach (var kvp in extra)
            {
                body[kvp.Key] = kvp.Value?.DeepClone();
            }
        }
        return Results.Text(body.ToJsonString(), ProblemContentType, statusCode: status);
    }

    /// <summary>
    /// Wrap <paramref name="payload"/> in the standard success envelope and
    /// return it as JSON.
    ///
    /// The envelope includes the bridge <c>compatibility_level</c> so clients
    /// can detect contract changes.
    /// </summary>
    public static IResult Ok(JsonNode? payload)
    {
        var body = new JsonObject
        {
            ["compatibility_level"] = Schemas.CompatibilityLevel,
            ["payload"] = payload?.DeepClone(),
        };
        return Results.Text(body.ToJsonString(), "application/json", statusCode: 200);
    }
}
