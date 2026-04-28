# Bridge - .NET

Reference implementation of the [CiviForm Bridge contract](../docs/howto.md) using ASP.NET Core minimal APIs and `JsonSchema.Net`.

## Requirements

- .NET SDK 9.0+ (`TargetFramework` is `net9.0` in `Bridge.csproj`)
- `jq` (only required to run `bin/test.sh`)

Verify the SDK:

```bash
dotnet --version   # expect 9.0.x
```

## Setup

```bash
cd dotnet
dotnet restore
```

NuGet packages (pinned in `Bridge.csproj`):

- `JsonSchema.Net` 9.2.0

## Run

```bash
./bin/run.sh
# or, equivalently:
dotnet run
```

The service listens on `http://0.0.0.0:5012` (configured via `WebHost.UseUrls` in `Program.cs`).

Quick smoke check:

```bash
curl -s http://localhost:5012/health-check | jq
curl -s http://localhost:5012/discovery   | jq
```

## Test

With the service running in another terminal:

```bash
./bin/test.sh
```

`BASE_URL` defaults to `http://localhost:5012` in this script.

## Layout

| File             | Purpose                                            |
| ---------------- | -------------------------------------------------- |
| `Program.cs`     | Host setup, route wiring, error handlers           |
| `Services.cs`    | Bridge slug dispatch and request/response envelope |
| `Schemas.cs`     | JSON Schemas + `/discovery` document               |
| `Validation.cs`  | Schema-based request payload validation            |
| `Responses.cs`   | RFC 9457 problem+json helpers                      |
| `Data.cs`        | In-memory sample customer records                  |
| `Bridge.csproj`  | SDK target, package references                     |
