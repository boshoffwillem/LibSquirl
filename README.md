# LibSquirl

A .NET library for [Turso](https://turso.tech/) / [libSQL](https://github.com/tursodatabase/libsql) providing two layers:

- **Protocol client** — Execute SQL against any libSQL database over the [Hrana over HTTP](https://github.com/tursodatabase/libsql/blob/main/docs/HRANA_3_SPEC.md) protocol (`POST /v2/pipeline`).
- **Platform client** — Manage Turso resources (organizations, groups, databases, tokens, etc.) via the [Turso Platform REST API](https://docs.turso.tech/api-reference).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.102+)

## Installation

Add a project reference or, when published as a NuGet package:

```bash
dotnet add package LibSquirl
```

## Quick Start

### Protocol Client — Execute SQL

```csharp
using LibSquirl.Protocol;
using LibSquirl.Protocol.Models;

HttpClient httpClient = new();
LibSqlClientOptions options = new()
{
    Url = "http://localhost:8080",   // or your Turso database URL
    AuthToken = "your-token"         // optional for local, required for Turso
};

ILibSqlClient client = new LibSqlClient(httpClient, options);

// Execute a single statement
StatementResult result = await client.ExecuteAsync(
    "SELECT * FROM users WHERE age > :age",
    namedArgs: [new NamedArg { Name = "age", Value = Value.Integer(21) }]
);

// Map results to a strongly-typed object
List<User> users = result.MapTo<User>();
User? first = result.MapToFirstOrDefault<User>();
```

### Platform Client — Manage Turso Resources

```csharp
using LibSquirl.Platform;
using LibSquirl.Platform.Models;

HttpClient httpClient = new();
TursoPlatformOptions options = new()
{
    ApiToken = "your-turso-api-token",
    OrganizationSlug = "my-org"
};

ITursoPlatformClient platform = new TursoPlatformClient(httpClient, options);

// List all databases
List<Database> databases = await platform.Databases.ListAsync();

// Create a new database
Database db = await platform.Databases.CreateAsync(new CreateDatabaseRequest
{
    Name = "my-new-db",
    Group = "default"
});
```

## Protocol Client API

The `ILibSqlClient` interface provides:

| Method | Description |
|---|---|
| `ExecuteAsync` | Execute a single SQL statement with positional or named arguments |
| `BatchAsync` | Execute a batch of statements with optional conditions |
| `SequenceAsync` | Execute semicolon-separated SQL statements |
| `DescribeAsync` | Parse and analyze a SQL statement |
| `PipelineAsync` | Send a raw pipeline request for full protocol control |
| `HealthCheckAsync` | Check the database server health |
| `GetVersionAsync` | Get the server version string |
| `DumpAsync` | Dump the entire database as SQL text |
| `ListMigrationJobsAsync` | List all schema migration jobs |
| `GetMigrationJobAsync` | Get details about a specific migration job |

### Value Types

SQL values are represented as a discriminated union via the `Value` base record:

```csharp
Value.Null()           // SQL NULL
Value.Integer(42)      // long
Value.Float(3.14)      // double
Value.Text("hello")    // string
Value.Blob(bytes)      // byte[]
```

### Result Mapping

Map `StatementResult` rows to C# objects with `MapTo<T>()` and `MapToFirstOrDefault<T>()`:

```csharp
public sealed class User
{
    [ColumnName("user_id")]      // explicit column mapping
    public int Id { get; set; }

    public string Name { get; set; } = "";  // matched case-insensitively

    public DateTime CreatedAt { get; set; }
}

StatementResult result = await client.ExecuteAsync("SELECT user_id, name, created_at FROM users");
List<User> users = result.MapTo<User>();
```

**Matching rules:**
1. `[ColumnName("col")]` on a property takes precedence
2. Otherwise matches by property name (case-insensitive)
3. Unmatched properties keep their default value; unmatched columns are ignored

**Supported type conversions:**

| Value Type | Target CLR Types |
|---|---|
| `IntegerValue` | `long`, `int`, `short`, `byte`, `bool` (0/1), `uint`, `ulong`, `ushort`, `enum`, and nullable variants |
| `FloatValue` | `double`, `float`, `decimal`, and nullable variants |
| `TextValue` | `string`, `DateTime`, `DateTimeOffset`, `Guid`, `enum` (case-insensitive), and nullable variants |
| `BlobValue` | `byte[]` |
| `NullValue` | Sets nullable/reference types to `null`; throws for non-nullable value types |

## Platform Client API

The `ITursoPlatformClient` aggregates sub-clients for each area of the Turso Platform API:

### Organizations (`platform.Organizations`)

List, get, and update organizations. Manage billing (plans, subscriptions, invoices, usage), members, and invites.

### Groups (`platform.Groups`)

Create, list, get, and delete groups. Manage group locations, auth tokens, configuration, transfers, and unarchiving.

### Databases (`platform.Databases`)

Create, list, get, and delete databases. Manage auth tokens, stats, instances, configuration, usage, and dump uploads.

### Locations (`platform.Locations`)

List all available locations and get the closest region.

### API Tokens (`platform.Tokens`)

List, create, validate, and revoke platform API tokens.

### Audit Logs (`platform.AuditLogs`)

List audit log entries with pagination.

## Configuration

### Protocol Client

```csharp
LibSqlClientOptions options = new()
{
    Url = "https://your-db-slug.turso.io",  // required
    AuthToken = "your-jwt-token"             // optional for local, required for Turso
};
```

### Platform Client

```csharp
TursoPlatformOptions options = new()
{
    BaseUrl = "https://api.turso.tech",  // default; override for testing
    ApiToken = "your-api-token",         // required
    OrganizationSlug = "my-org"          // required
};
```

## Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for integration tests)

### Build

```bash
dotnet build
```

### Test

Integration tests run against a local [libsql-server](https://github.com/tursodatabase/libsql) Docker container:

```bash
# Start the local libsql-server
docker compose up -d

# Run all tests
dotnet test

# Stop the container
docker compose down
```

### Project Structure

```
LibSquirl/
├── Protocol/              # libSQL Hrana-over-HTTP protocol client
│   ├── Models/            # Value, Statement, Batch, StreamRequest, etc.
│   ├── ILibSqlClient.cs   # Protocol client interface
│   ├── LibSqlClient.cs    # Implementation
│   └── ...
├── Platform/              # Turso Platform REST API wrapper
│   ├── Models/            # Organization, Group, Database, Member, etc.
│   ├── Organizations/     # IOrganizationsApi + OrganizationsApi
│   ├── Groups/            # IGroupsApi + GroupsApi
│   ├── Databases/         # IDatabasesApi + DatabasesApi
│   ├── Locations/         # ILocationsApi + LocationsApi
│   ├── Tokens/            # IApiTokensApi + ApiTokensApi
│   ├── AuditLogs/         # IAuditLogsApi + AuditLogsApi
│   └── ...
LibSquirl.Tests/
├── Protocol/              # Integration tests (Docker) + unit tests
├── Platform/              # Unit tests with mock HTTP handler
docker-compose.yml         # libsql-server for integration tests
```
