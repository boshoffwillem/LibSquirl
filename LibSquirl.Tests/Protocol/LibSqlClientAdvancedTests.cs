using LibSquirl.Protocol;
using LibSquirl.Protocol.Models;

namespace LibSquirl.Tests.Protocol;

[Collection("LibSqlServer")]
public class LibSqlClientAdvancedTests(LibSqlServerFixture fixture)
{
    private readonly ILibSqlClient _client = fixture.Client;

    [Fact]
    public async Task SequenceAsync_MultipleStatements_ExecutesAll()
    {
        string tableName = $"test_seq_{Guid.NewGuid():N}";
        try
        {
            await _client.SequenceAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, val TEXT); "
                    + $"INSERT INTO {tableName} (val) VALUES ('a'); "
                    + $"INSERT INTO {tableName} (val) VALUES ('b');"
            );

            StatementResult result = await _client.ExecuteAsync(
                $"SELECT COUNT(*) FROM {tableName}"
            );
            Assert.Equal(2L, ((IntegerValue)result.Rows[0][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task SequenceAsync_InvalidSql_ThrowsException()
    {
        await Assert.ThrowsAsync<LibSqlException>(() =>
            _client.SequenceAsync("INVALID SQL STATEMENT;")
        );
    }

    [Fact]
    public async Task DescribeAsync_SelectStatement_ReturnsColumnsAndParams()
    {
        string tableName = $"test_desc_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)"
            );

            DescribeResult result = await _client.DescribeAsync(
                $"SELECT id, name, age FROM {tableName} WHERE age > ?"
            );

            Assert.True(result.IsReadonly);
            Assert.False(result.IsExplain);
            Assert.Equal(3, result.Cols.Count);
            Assert.Equal("id", result.Cols[0].Name);
            Assert.Equal("name", result.Cols[1].Name);
            Assert.Equal("age", result.Cols[2].Name);
            Assert.NotEmpty(result.Params);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task DescribeAsync_InsertStatement_IsNotReadonly()
    {
        string tableName = $"test_desc_ins_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, val TEXT)"
            );

            DescribeResult result = await _client.DescribeAsync(
                $"INSERT INTO {tableName} (val) VALUES (?)"
            );

            Assert.False(result.IsReadonly);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task PipelineAsync_StoreSqlAndExecute_Works()
    {
        string tableName = $"test_store_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, val TEXT)"
            );

            // Store SQL, then execute using sql_id, then close sql
            PipelineRequest request = new()
            {
                Requests =
                [
                    StreamRequest.StoreSql(1, $"INSERT INTO {tableName} (val) VALUES ('stored')"),
                    StreamRequest.Execute(new Statement { SqlId = 1 }),
                    StreamRequest.CloseSql(1),
                    StreamRequest.Close(),
                ],
            };

            PipelineResponse response = await _client.PipelineAsync(request);

            Assert.Equal(4, response.Results.Count);
            Assert.True(response.Results[0].IsOk);
            Assert.True(response.Results[1].IsOk);

            StatementResult selectResult = await _client.ExecuteAsync(
                $"SELECT val FROM {tableName}"
            );
            Assert.Equal("stored", ((TextValue)selectResult.Rows[0][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task PipelineAsync_BatonReuse_MaintainsStreamState()
    {
        string tableName = $"test_baton_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, val TEXT)"
            );

            // First pipeline: BEGIN transaction (no close, so we get a baton)
            PipelineRequest beginRequest = new()
            {
                Requests =
                [
                    StreamRequest.Execute(new Statement { Sql = "BEGIN" }),
                    StreamRequest.Execute(
                        new Statement { Sql = $"INSERT INTO {tableName} (val) VALUES ('in-tx')" }
                    ),
                ],
            };

            PipelineResponse beginResponse = await _client.PipelineAsync(beginRequest);
            Assert.NotNull(beginResponse.Baton);
            Assert.True(beginResponse.Results[0].IsOk);
            Assert.True(beginResponse.Results[1].IsOk);

            // Second pipeline: COMMIT using the baton
            PipelineRequest commitRequest = new()
            {
                Baton = beginResponse.Baton,
                Requests =
                [
                    StreamRequest.Execute(new Statement { Sql = "COMMIT" }),
                    StreamRequest.Close(),
                ],
            };

            PipelineResponse commitResponse = await _client.PipelineAsync(commitRequest);
            Assert.True(commitResponse.Results[0].IsOk);

            StatementResult result = await _client.ExecuteAsync($"SELECT val FROM {tableName}");
            Assert.Single(result.Rows);
            Assert.Equal("in-tx", ((TextValue)result.Rows[0][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task HealthCheckAsync_ReturnsTrue()
    {
        bool result = await _client.HealthCheckAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task GetVersionAsync_ReturnsNonEmptyString()
    {
        string version = await _client.GetVersionAsync();
        Assert.False(string.IsNullOrWhiteSpace(version));
    }

    [Fact]
    public async Task DumpAsync_ReturnsNonEmptyString()
    {
        // Ensure at least one table exists for the dump
        string tableName = $"test_dump_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync($"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY)");
            await _client.ExecuteAsync($"INSERT INTO {tableName} (id) VALUES (1)");

            string dump = await _client.DumpAsync();

            Assert.False(string.IsNullOrWhiteSpace(dump));
            Assert.Contains("CREATE TABLE", dump, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteAsync_ResponseFields_ContainsRowsReadAndWritten()
    {
        string tableName = $"test_fields_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, val TEXT)"
            );

            StatementResult insertResult = await _client.ExecuteAsync(
                $"INSERT INTO {tableName} (val) VALUES ('test')"
            );

            Assert.Equal(1, insertResult.AffectedRowCount);
            Assert.True(long.Parse(insertResult.LastInsertRowId!) > 0);

            StatementResult selectResult = await _client.ExecuteAsync($"SELECT * FROM {tableName}");

            Assert.Single(selectResult.Rows);
            Assert.Equal(2, selectResult.Cols.Count);
            Assert.Equal("id", selectResult.Cols[0].Name);
            Assert.Equal("val", selectResult.Cols[1].Name);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }
}
