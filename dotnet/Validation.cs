// <summary>
// JSON Schema validation for bridge request and response payloads.
// </summary>

using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Pointer;
using Json.Schema;

namespace Bridge;

public record ValidationError(string Name, string Message);

public static class Validation
{
    private static JsonSchema Compile(JsonObject schema) =>
        JsonSerializer.Deserialize<JsonSchema>(schema.ToJsonString())!;

    private static readonly Dictionary<string, JsonSchema> RequestValidators = new()
    {
        [Schemas.BridgeSlug] = Compile(Schemas.BuildRequestSchema()),
        [Schemas.LookupBridgeSlug] = Compile(Schemas.BuildLookupRequestSchema()),
    };

    private static readonly Dictionary<string, JsonSchema> ResponseValidators = new()
    {
        [Schemas.BridgeSlug] = Compile(Schemas.BuildResponseSchema()),
        [Schemas.LookupBridgeSlug] = Compile(Schemas.BuildLookupResponseSchema()),
    };

    /// <summary>
    /// Run <paramref name="schema"/> over <paramref name="payload"/> and return
    /// <see cref="ValidationError"/> records, empty on success.
    ///
    /// Splits the JsonSchema.Net <c>required</c> error (which lists every
    /// missing property in one message) into one error per property so the
    /// caller sees a per-field validation envelope. Surfaces
    /// <c>additionalProperties</c> failures using the offending property name
    /// extracted from the instance location.
    /// </summary>
    private static List<ValidationError> FormatErrors(JsonSchema schema, JsonNode? payload)
    {
        var json = payload is null ? "null" : payload.ToJsonString();
        using var doc = JsonDocument.Parse(json);
        var options = new EvaluationOptions { OutputFormat = OutputFormat.List };
        var results = schema.Evaluate(doc.RootElement, options);

        var errors = new List<ValidationError>();
        if (results.IsValid) return errors;

        foreach (var detail in Flatten(results))
        {
            if (detail.Errors is null || detail.Errors.Count == 0) continue;
            foreach (var kv in detail.Errors)
            {
                var keyword = kv.Key;
                var message = kv.Value;
                var evalPath = detail.EvaluationPath.ToString();

                if (string.IsNullOrEmpty(keyword) && evalPath.EndsWith("/additionalProperties"))
                {
                    var name = LastSegment(detail.InstanceLocation);
                    var label = name ?? "payload";
                    errors.Add(new ValidationError(label, $"additional property '{label}' is not allowed"));
                    continue;
                }

                if (keyword == "required")
                {
                    var names = ExtractRequiredFromMessage(message);
                    if (names.Count == 0)
                    {
                        errors.Add(new ValidationError("payload", message));
                    }
                    else
                    {
                        foreach (var name in names)
                        {
                            errors.Add(new ValidationError(name, message));
                        }
                    }
                    continue;
                }

                errors.Add(new ValidationError(FirstSegment(detail.InstanceLocation) ?? "payload", message));
            }
        }
        return errors;
    }

    /// <summary>Recursively yield every <see cref="EvaluationResults"/> in the tree.</summary>
    private static IEnumerable<EvaluationResults> Flatten(EvaluationResults results)
    {
        yield return results;
        if (results.Details is null) yield break;
        foreach (var child in results.Details)
        {
            foreach (var grand in Flatten(child)) yield return grand;
        }
    }

    private static string? FirstSegment(JsonPointer pointer) =>
        pointer.SegmentCount == 0 ? null : pointer[0].ToString();

    private static string? LastSegment(JsonPointer pointer) =>
        pointer.SegmentCount == 0 ? null : pointer[pointer.SegmentCount - 1].ToString();

    /// <summary>
    /// Parse the JSON array embedded in a <c>required</c> error message and
    /// return the missing property names.
    /// </summary>
    private static List<string> ExtractRequiredFromMessage(string message)
    {
        var start = message.IndexOf('[');
        var end = message.IndexOf(']');
        if (start < 0 || end <= start) return new();
        var inner = message.Substring(start, end - start + 1);
        try
        {
            using var doc = JsonDocument.Parse(inner);
            var names = new List<string>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var name = element.GetString();
                if (!string.IsNullOrEmpty(name)) names.Add(name);
            }
            return names;
        }
        catch (JsonException)
        {
            return new();
        }
    }

    /// <summary>
    /// Validate a request <paramref name="payload"/> against the schema for
    /// <paramref name="slug"/>. Returns a list of <see cref="ValidationError"/>
    /// records, empty on success.
    /// </summary>
    public static List<ValidationError> ValidateRequest(string slug, JsonNode? payload) =>
        FormatErrors(RequestValidators[slug], payload);

    /// <summary>
    /// Validate a response <paramref name="payload"/> against the schema for
    /// <paramref name="slug"/>. Returns a list of <see cref="ValidationError"/>
    /// records, empty on success.
    /// </summary>
    public static List<ValidationError> ValidateResponse(string slug, JsonNode? payload) =>
        FormatErrors(ResponseValidators[slug], payload);
}
