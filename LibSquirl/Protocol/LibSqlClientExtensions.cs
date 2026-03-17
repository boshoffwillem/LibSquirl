using LibSquirl.Protocol.Models;

namespace LibSquirl.Protocol;

/// <summary>
///     Extension methods for <see cref="ILibSqlClient" /> providing high-level
///     batch execution and convenience APIs.
/// </summary>
public static class LibSqlClientExtensions
{
    /// <summary>
    ///     Executes multiple SQL statements in a single HTTP request using the batch API.
    ///     Each statement gets its own <see cref="StatementResult" /> in the returned list,
    ///     which can be mapped via <see cref="StatementResultExtensions.MapTo{T}" />.
    /// </summary>
    /// <param name="client">The LibSQL client.</param>
    /// <param name="statements">
    ///     A list of (SQL, NamedArgs) tuples. Each tuple becomes one batch step.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     A list of <see cref="StatementResult" />, one per input statement, in the same order.
    /// </returns>
    /// <exception cref="LibSqlException">Thrown if any batch step returns an error.</exception>
    public static async Task<IReadOnlyList<StatementResult>> ExecuteMultipleAsync(
        this ILibSqlClient client,
        IReadOnlyList<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements,
        CancellationToken cancellationToken = default
    )
    {
        if (statements.Count == 0)
        {
            return [];
        }

        // Single statement — use the simpler ExecuteAsync path
        if (statements.Count == 1)
        {
            (string sql, IReadOnlyList<NamedArg>? namedArgs) = statements[0];
            StatementResult result = await client.ExecuteAsync(
                sql,
                namedArgs: namedArgs,
                cancellationToken: cancellationToken
            );
            return [result];
        }

        Batch batch = new() { Steps = new List<BatchStep>(statements.Count) };

        for (int i = 0; i < statements.Count; i++)
        {
            (string sql, IReadOnlyList<NamedArg>? namedArgs) = statements[i];

            Statement stmt = new() { Sql = sql };

            if (namedArgs is { Count: > 0 })
            {
                stmt.NamedArgs = [.. namedArgs];
            }

            batch.Steps.Add(new BatchStep { Stmt = stmt });
        }

        BatchResult batchResult = await client.BatchAsync(batch, cancellationToken);

        // Validate results — throw on any step error
        StatementResult[] results = new StatementResult[statements.Count];

        for (int i = 0; i < statements.Count; i++)
        {
            if (i < batchResult.StepErrors.Count && batchResult.StepErrors[i] is { } error)
            {
                throw new LibSqlException($"Batch step {i} failed: [{error.Code}] {error.Message}");
            }

            results[i] =
                batchResult.StepResults[i]
                ?? throw new LibSqlException($"Batch step {i} returned null result");
        }

        return results;
    }
}
