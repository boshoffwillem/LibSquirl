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
├── Sql.cs                       # Convenience NamedArg factories & IN-clause builders
├── Protocol/                    # libSQL HTTP v2 protocol client
│   ├── Models/                  # Value, Statement, Batch, StreamRequest, etc.
│   ├── ILibSqlClient.cs         # Protocol client interface
│   ├── LibSqlClient.cs          # Implementation
│   ├── LibSqlClientOptions.cs   # URL + auth token config
│   ├── LibSqlClientExtensions.cs # ExecuteMultipleAsync batch extension
│   ├── LibSqlException.cs       # Custom exception
│   ├── ColumnNameAttribute.cs   # [ColumnName] for result mapping
│   ├── StatementResultExtensions.cs  # MapTo<T>(), MapToFirstOrDefault<T>()
│   └── StatementResultMapper.cs # Internal: cached reflection + type conversion
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
├── SqlTests.cs                  # Unit tests for Sql helper (no Docker needed)
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

### SQL parameter helpers (`Sql` static class)

The `Sql` static class in the root namespace provides convenience factory methods for building `NamedArg` parameters and SQL IN-clause placeholders, eliminating verbose `new NamedArg { Name, Value }` boilerplate:

```csharp
using LibSquirl;

// Single-value args — type-safe, auto-formatted
Sql.Arg(":name", "alice")              // → Value.Text("alice")
Sql.Arg(":id", someGuid)              // → Value.Text(guid.ToString())
Sql.Arg(":age", 42)                   // → Value.Integer(42)
Sql.Arg(":active", true)              // → Value.Integer(1)
Sql.Arg(":score", 3.14)               // → Value.Float(3.14)
Sql.Arg(":amount", 99.9m)             // → Value.Text("99.90") — F2 format
Sql.Arg(":created", dateTime)          // → Value.Text("2024-06-15T10:30:45.123Z") — UTC ISO 8601

// Nullable args — Value.Null() when null
Sql.ArgNullable(":ref", (Guid?)null)   // → Value.Null()
Sql.ArgNullable(":ref", (Guid?)id)     // → Value.Text(id.ToString())
// Supports: string?, Guid?, int?, long?, double?, decimal?, DateTime?

// IN-clause builders — returns (placeholders, args) tuple
var (inSql, inArgs) = Sql.In(":id", guids);     // → (":id0, :id1, :id2", NamedArg[])
var (inSql, inArgs) = Sql.In(":s", strings);     // same for string lists
var (inSql, inArgs) = Sql.In(":id", emptyList);  // → ("1=0", []) — always valid SQL

// Combine fixed args with IN-clause args
NamedArg[] allArgs = Sql.CombineArgs(inArgs, Sql.Arg(":status", "Active"));

// Usage in query
StatementResult result = await client.ExecuteAsync(
    $"SELECT * FROM Products WHERE Id IN ({inSql}) AND Status = :status",
    namedArgs: allArgs
);
```

### Batch execution (`ExecuteMultipleAsync` extension)

The `LibSqlClientExtensions` class provides `ExecuteMultipleAsync`, a high-level extension on `ILibSqlClient` that sends multiple SQL statements in a single HTTP request using the batch API:

```csharp
using LibSquirl.Protocol;

List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements =
[
    ("SELECT * FROM Products WHERE ProducerId = :pid", [Sql.Arg(":pid", producerId)]),
    ("SELECT * FROM Categories", null),
    ("SELECT * FROM PickupPoints WHERE ProducerId = :pid", [Sql.Arg(":pid", producerId)]),
];

IReadOnlyList<StatementResult> results = await client.ExecuteMultipleAsync(statements);

List<Product> products = results[0].MapTo<Product>();
List<Category> categories = results[1].MapTo<Category>();
List<PickupPoint> points = results[2].MapTo<PickupPoint>();
```

**Behavior:**
- Empty list → returns `[]`
- Single statement → falls through to `ExecuteAsync` (no batch overhead)
- Multiple statements → wraps in `BatchAsync` (single HTTP POST)
- Throws `LibSqlException` if any step returns an error (includes step index in message)
- Each `StatementResult` in the returned list supports `MapTo<T>()` and `MapToFirstOrDefault<T>()`

### Result-to-type mapping

`StatementResult` has extension methods to map rows to strongly-typed C# objects:

```csharp
StatementResult result = await client.ExecuteAsync("SELECT * FROM users");
List<User> users = result.MapTo<User>();
User? first = result.MapToFirstOrDefault<User>();
```

**Column-to-property matching rules:**

1. If `[ColumnName("col")]` is present on the property, match against that name
2. Otherwise, match by property name (case-insensitive) against column names
3. Properties without a matching column are left at their default value
4. Columns without a matching property are silently ignored

**Supported type conversions:**

| Value Type | Target CLR Types |
|---|---|
| `IntegerValue(long)` | `long`, `int`, `short`, `byte`, `bool` (0/1), `uint`, `ulong`, `ushort`, `enum`, and nullable variants |
| `FloatValue(double)` | `double`, `float`, `decimal`, and nullable variants |
| `TextValue(string)` | `string`, `DateTime`, `DateTimeOffset`, `Guid`, `enum` (case-insensitive), and nullable variants |
| `BlobValue(byte[])` | `byte[]` |
| `NullValue` | `null` for nullable/reference types; throws `InvalidOperationException` for non-nullable value types |

**Key implementation details:**

- `StatementResultMapper` is `internal static` — reflection metadata is cached per type in a `ConcurrentDictionary`
- `StatementResultExtensions` is the public API surface — extension methods on `StatementResult`
- Target type `T` must have a parameterless constructor (`where T : new()`)
- `MapToFirstOrDefault<T>` additionally requires `where T : class`

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
