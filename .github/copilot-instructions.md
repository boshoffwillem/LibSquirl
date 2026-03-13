# LibSquirl — Copilot Instructions

## Project Overview

LibSquirl is a C# class library providing two layers:

1. **Protocol layer** (`LibSquirl/Protocol/`) — Implements the libSQL HTTP v2 protocol (Hrana over HTTP) for executing SQL against Turso/libSQL databases via `POST /v2/pipeline`.
2. **Platform layer** (`LibSquirl/Platform/`) — Wraps the Turso Platform REST API for managing organizations, groups, databases, locations, API tokens, members, invites, and audit logs.

## Tech Stack & Constraints

- **.NET 10** (`net10.0`), SDK 10.0.102
- **System.Text.Json** only — no Newtonsoft.Json or other third-party JSON libraries
- **Central package management** via `Directory.Packages.props` — all NuGet versions are declared there, not in individual `.csproj` files
- **xUnit 2.9.3** for testing — uses `Task` return types for `IAsyncLifetime` (not `ValueTask`, which is xUnit v3)
- **No third-party HTTP libraries** — use `HttpClient` directly

## Project Structure

```
LibSquirl/
├── Protocol/                    # libSQL HTTP v2 protocol client
│   ├── Models/                  # Value, Statement, Batch, StreamRequest, etc.
│   ├── ILibSqlClient.cs         # Protocol client interface
│   ├── LibSqlClient.cs          # Implementation
│   ├── LibSqlClientOptions.cs   # URL + auth token config
│   └── LibSqlException.cs       # Custom exception
├── Platform/                    # Turso Platform REST API wrapper
│   ├── Models/                  # Organization, Group, Database, Member, etc.
│   ├── Organizations/           # IOrganizationsApi + OrganizationsApi
│   ├── Groups/                  # IGroupsApi + GroupsApi
│   ├── Databases/               # IDatabasesApi + DatabasesApi
│   ├── Locations/               # ILocationsApi + LocationsApi
│   ├── Tokens/                  # IApiTokensApi + ApiTokensApi
│   ├── AuditLogs/               # IAuditLogsApi + AuditLogsApi
│   ├── PlatformApiBase.cs       # Base class with GetAsync/PostAsync/PatchAsync/DeleteAsync helpers
│   ├── ITursoPlatformClient.cs  # Aggregator interface
│   ├── TursoPlatformClient.cs   # Aggregator implementation
│   ├── TursoPlatformOptions.cs  # BaseUrl, ApiToken, OrgSlug
│   └── TursoPlatformException.cs
└── Serialization/               # Shared serialization helpers (if any)

LibSquirl.Tests/
├── Protocol/                    # Integration tests (Docker libsql-server) + unit tests
│   ├── LibSqlServerFixture.cs   # xUnit collection fixture for Docker container
│   └── *Tests.cs
├── Platform/                    # Unit tests with mock HTTP
│   ├── MockHttpMessageHandler.cs
│   └── {SubApi}/                # One folder per sub-API
└── docker-compose.yml           # libsql-server container for integration tests
```

## Architecture Patterns

### Platform API sub-clients

Each Platform API area follows this pattern:

1. **Interface** (`I{Area}Api.cs`) — defines all async methods
2. **Implementation** (`{Area}Api.cs`) — extends `PlatformApiBase`, uses primary constructors
3. **Models** in `Platform/Models/` — shared across sub-APIs
4. **Private wrapper classes** — nested inside the implementation for deserializing JSON envelope responses (e.g., `{"group": {...}}`)

Example:

```csharp
public sealed class GroupsApi(HttpClient httpClient, TursoPlatformOptions options)
    : PlatformApiBase(httpClient, options), IGroupsApi
{
    public async Task<Group> GetAsync(string groupName, CancellationToken ct = default)
    {
        GroupWrapper wrapper = await GetAsync<GroupWrapper>($"{GroupsPath}/{groupName}", ct);
        return wrapper.Group;
    }

    private sealed class GroupWrapper
    {
        [JsonPropertyName("group")]
        public Group Group { get; set; } = null!;
    }
}
```

### Protocol layer

- All communication goes through `POST /v2/pipeline` with baton-based stateful streams
- Polymorphic JSON serialization uses custom `JsonConverter<T>` implementations (not `[JsonDerivedType]`)
- Value types are discriminated by a `"type"` field: `null`, `integer`, `float`, `text`, `blob`
- libSQL returns **unpadded base64** for blobs — use the `PadBase64()` helper in `ValueConverter`

## Coding Conventions

Follow the `.editorconfig` in the repository root. Key rules:

- **File-scoped namespaces**: `namespace LibSquirl.Protocol;`
- **Primary constructors** for classes with simple DI (e.g., API classes)
- **`sealed`** on all concrete classes that aren't designed for inheritance
- **No `var`** — use explicit types: `string name = ...`, `List<Group> groups = ...`
- **Private instance fields**: `_camelCase` prefix (e.g., `_httpClient`)
- **Private static fields**: `s_camelCase` prefix (e.g., `s_jsonOptions`)
- **Constants**: `PascalCase` for both public and private
- **Separate import directive groups**: System usings first, then project usings, separated by blank line
- **Allman braces**: opening brace on new line for all blocks
- **`CancellationToken`** as last parameter with `default` value on all async public methods
- **`JsonPropertyName`** attributes on all model properties — do not rely on naming policy for models
- **`JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)`** on optional/nullable properties
- Only comment code that needs clarification; do not add obvious comments

## JSON Serialization

- Use `JsonNamingPolicy.SnakeCaseLower` on `JsonSerializerOptions` for request/response serialization
- Use `[JsonPropertyName("...")]` on all model properties for explicit mapping
- Use `[JsonConverter(typeof(...))]` for polymorphic types (Value, StreamRequest, StreamResult, BatchCondition)
- Set `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` on serializer options
- Turso Database API returns PascalCase for some fields (`Name`, `DbId`, `Hostname`) — model attributes must match exactly

## Testing Conventions

### Platform API tests (unit tests with mocks)

- Use `MockHttpMessageHandler` from `LibSquirl.Tests/Platform/MockHttpMessageHandler.cs`
- Each test class has a static `CreateApi()` factory returning `(ApiImpl, MockHttpMessageHandler)`
- Enqueue responses with `handler.EnqueueResponse(HttpStatusCode.OK, jsonString)`
- Assert both the **deserialized result** and the **recorded request** (method, path, body)
- Test error cases: assert `TursoPlatformException` with correct `StatusCode`

### Protocol tests (integration tests with Docker)

- Use `[Collection("LibSqlServer")]` and inject `LibSqlServerFixture`
- Each test creates unique table names using `$"test_{purpose}_{Guid.NewGuid():N}"` to avoid collisions
- Always clean up tables in a `finally` block
- For endpoints not supported by local libsql-server (e.g., `/v1/jobs`), use a simple mock handler

### Running tests

```bash
# Start the local libsql-server
docker compose up -d

# Run all tests
dotnet test

# Stop the container
docker compose down
```

## Adding a New Platform API Endpoint

1. Add the model(s) to `LibSquirl/Platform/Models/`
2. Add the method signature to the interface (`I{Area}Api.cs`)
3. Implement the method in `{Area}Api.cs` — add any needed private wrapper classes
4. If it's a new HTTP method, ensure `PlatformApiBase` supports it
5. Add a test in `LibSquirl.Tests/Platform/{Area}/{Area}ApiTests.cs`
6. Verify: `dotnet build && dotnet test`

## Adding a New Protocol Endpoint

1. Add model(s) to `LibSquirl/Protocol/Models/`
2. Add the method signature to `ILibSqlClient.cs`
3. Implement in `LibSqlClient.cs`
4. Add integration test in `LibSquirl.Tests/Protocol/` (or mock test if not supported locally)
5. Verify: `dotnet build && dotnet test`
