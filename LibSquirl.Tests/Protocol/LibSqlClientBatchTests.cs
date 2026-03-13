using LibSquirl.Protocol;
using LibSquirl.Protocol.Models;

namespace LibSquirl.Tests.Protocol;

[Collection("LibSqlServer")]
public class LibSqlClientBatchTests(LibSqlServerFixture fixture)
{
    private readonly ILibSqlClient _client = fixture.Client;

    [Fact]
    public async Task BatchAsync_MultipleStatements_ExecutesAll()
    {
        string tableName = $"test_batch_{Guid.NewGuid():N}";
        try
        {
            Batch batch = new()
            {
                Steps =
                [
                    new BatchStep { Stmt = new Statement { Sql = $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT)" } },
                    new BatchStep { Stmt = new Statement { Sql = $"INSERT INTO {tableName} (name) VALUES ('a')" } },
                    new BatchStep { Stmt = new Statement { Sql = $"INSERT INTO {tableName} (name) VALUES ('b')" } },
                    new BatchStep { Stmt = new Statement { Sql = $"SELECT COUNT(*) FROM {tableName}" } },
                ]
            };

            BatchResult result = await _client.BatchAsync(batch);

            Assert.Equal(4, result.StepResults.Count);
            Assert.All(result.StepErrors, error => Assert.Null(error));

            StatementResult? countResult = result.StepResults[3];
            Assert.NotNull(countResult);
            Assert.Equal(2L, ((IntegerValue)countResult.Rows[0][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task BatchAsync_OkCondition_ExecutesConditionally()
    {
        string tableName = $"test_batch_ok_{Guid.NewGuid():N}";
        try
        {
            Batch batch = new()
            {
                Steps =
                [
                    new BatchStep
                    {
                        Stmt = new Statement { Sql = $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT)" }
                    },
                    new BatchStep
                    {
                        Condition = BatchCondition.Ok(0),
                        Stmt = new Statement { Sql = $"INSERT INTO {tableName} (name) VALUES ('conditional')" }
                    },
                    new BatchStep
                    {
                        Condition = BatchCondition.Ok(1),
                        Stmt = new Statement { Sql = $"SELECT * FROM {tableName}" }
                    },
                ]
            };

            BatchResult result = await _client.BatchAsync(batch);

            Assert.All(result.StepErrors, error => Assert.Null(error));
            StatementResult? selectResult = result.StepResults[2];
            Assert.NotNull(selectResult);
            Assert.Single(selectResult.Rows);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task BatchAsync_ErrorCondition_HandlesFailure()
    {
        string tableName = $"test_batch_err_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");

            Batch batch = new()
            {
                Steps =
                [
                    // This should fail (NULL into NOT NULL column)
                    new BatchStep
                    {
                        Stmt = new Statement { Sql = $"INSERT INTO {tableName} (name) VALUES (NULL)" }
                    },
                    // This should run because step 0 errored
                    new BatchStep
                    {
                        Condition = BatchCondition.Error(0),
                        Stmt = new Statement { Sql = $"INSERT INTO {tableName} (name) VALUES ('fallback')" }
                    },
                    // This should NOT run because step 0 didn't succeed
                    new BatchStep
                    {
                        Condition = BatchCondition.Ok(0),
                        Stmt = new Statement { Sql = $"INSERT INTO {tableName} (name) VALUES ('should-not-exist')" }
                    },
                ]
            };

            BatchResult result = await _client.BatchAsync(batch);

            Assert.NotNull(result.StepErrors[0]);
            Assert.NotNull(result.StepResults[1]);
            Assert.Null(result.StepResults[2]); // Skipped

            StatementResult selectResult = await _client.ExecuteAsync(
                $"SELECT name FROM {tableName}");
            Assert.Single(selectResult.Rows);
            Assert.Equal("fallback", ((TextValue)selectResult.Rows[0][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task BatchAsync_NotCondition_InvertsLogic()
    {
        string tableName = $"test_batch_not_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT)");

            Batch batch = new()
            {
                Steps =
                [
                    // This succeeds
                    new BatchStep
                    {
                        Stmt = new Statement { Sql = $"INSERT INTO {tableName} (name) VALUES ('first')" }
                    },
                    // NOT ok(0) -> false, so this should be skipped
                    new BatchStep
                    {
                        Condition = BatchCondition.Not(BatchCondition.Ok(0)),
                        Stmt = new Statement { Sql = $"INSERT INTO {tableName} (name) VALUES ('should-skip')" }
                    },
                ]
            };

            BatchResult result = await _client.BatchAsync(batch);

            Assert.Null(result.StepResults[1]); // Skipped

            StatementResult selectResult = await _client.ExecuteAsync(
                $"SELECT name FROM {tableName}");
            Assert.Single(selectResult.Rows);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task BatchAsync_Transaction_CommitsOnSuccess()
    {
        string tableName = $"test_batch_tx_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, val INTEGER)");

            Batch batch = new()
            {
                Steps =
                [
                    new BatchStep { Stmt = new Statement { Sql = "BEGIN" } },
                    new BatchStep
                    {
                        Condition = BatchCondition.Ok(0),
                        Stmt = new Statement { Sql = $"INSERT INTO {tableName} (val) VALUES (1)" }
                    },
                    new BatchStep
                    {
                        Condition = BatchCondition.Ok(1),
                        Stmt = new Statement { Sql = $"INSERT INTO {tableName} (val) VALUES (2)" }
                    },
                    new BatchStep
                    {
                        Condition = BatchCondition.Ok(2),
                        Stmt = new Statement { Sql = "COMMIT" }
                    },
                    // Rollback if anything failed
                    new BatchStep
                    {
                        Condition = BatchCondition.Not(BatchCondition.Ok(2)),
                        Stmt = new Statement { Sql = "ROLLBACK" }
                    },
                ]
            };

            BatchResult result = await _client.BatchAsync(batch);
            Assert.All(result.StepErrors.Take(4), error => Assert.Null(error));

            StatementResult selectResult = await _client.ExecuteAsync(
                $"SELECT COUNT(*) FROM {tableName}");
            Assert.Equal(2L, ((IntegerValue)selectResult.Rows[0][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task BatchAsync_WithPositionalArgs_Works()
    {
        string tableName = $"test_batch_args_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT)");

            Batch batch = new()
            {
                Steps =
                [
                    new BatchStep
                    {
                        Stmt = new Statement
                        {
                            Sql = $"INSERT INTO {tableName} (name) VALUES (?)",
                            Args = [Value.Text("from-batch")]
                        }
                    },
                ]
            };

            BatchResult result = await _client.BatchAsync(batch);
            Assert.Null(result.StepErrors[0]);

            StatementResult selectResult = await _client.ExecuteAsync(
                $"SELECT name FROM {tableName}");
            Assert.Equal("from-batch", ((TextValue)selectResult.Rows[0][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }
}
