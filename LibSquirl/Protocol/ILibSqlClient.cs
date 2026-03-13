using LibSquirl.Protocol.Models;

namespace LibSquirl.Protocol;

public interface ILibSqlClient
{
    /// Executes a single SQL statement and returns the result.
    Task<StatementResult> ExecuteAsync(
        string sql,
        IReadOnlyList<Value>? args = null,
        IReadOnlyList<NamedArg>? namedArgs = null,
        CancellationToken cancellationToken = default);

    /// Executes a batch of statements with optional conditions.
    Task<BatchResult> BatchAsync(
        Batch batch,
        CancellationToken cancellationToken = default);

    /// Executes a sequence of semicolon-separated SQL statements.
    Task SequenceAsync(
        string sql,
        CancellationToken cancellationToken = default);

    /// Describes (parses/analyzes) a SQL statement.
    Task<DescribeResult> DescribeAsync(
        string sql,
        CancellationToken cancellationToken = default);

    /// Sends a raw pipeline request for full control over the protocol.
    Task<PipelineResponse> PipelineAsync(
        PipelineRequest request,
        CancellationToken cancellationToken = default);

    /// Checks the health of the database server.
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);

    /// Gets the server version.
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);

    /// Dumps the entire database as SQL text.
    Task<string> DumpAsync(CancellationToken cancellationToken = default);

    /// Lists all schema migration jobs.
    Task<MigrationJobsSummary> ListMigrationJobsAsync(CancellationToken cancellationToken = default);

    /// Gets details about a specific migration job.
    Task<MigrationJobDetail> GetMigrationJobAsync(int jobId, CancellationToken cancellationToken = default);
}
