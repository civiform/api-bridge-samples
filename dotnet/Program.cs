// <summary>
// ASP.NET Core application entry point exposing the bridge HTTP endpoints.
// </summary>

using System.Text.Json;
using System.Text.Json.Nodes;
using Bridge;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.WebHost.UseUrls("http://0.0.0.0:5012");

var app = builder.Build();

/// <summary>Return the current server time as a liveness signal.</summary>
app.MapGet("/health-check", () => Results.Json(new
{
    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
}));

/// <summary>Return the bridge discovery document describing every endpoint.</summary>
app.MapGet("/discovery", () =>
    Results.Text(Schemas.DiscoveryDocument().ToJsonString(), "application/json"));

/// <summary>Dispatch a bridge request identified by <c>slug</c> to its handler.</summary>
app.MapPost("/bridge/{slug}", async (string slug, HttpRequest request) =>
{
    JsonNode? body = null;
    try
    {
        if (request.ContentLength != 0)
        {
            body = await JsonNode.ParseAsync(request.Body);
        }
    }
    catch (JsonException) { /* fall through with null body */ }

    return Services.HandleBridgeRequest(slug, body);
});

app.Run();
