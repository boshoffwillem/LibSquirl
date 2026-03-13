using LibSquirl.Protocol;
using LibSquirl.Protocol.Models;

namespace LibSquirl.Tests.Protocol;

[Collection("LibSqlServer")]
public class LibSqlClientExecuteTests(LibSqlServerFixture fixture)
{
    private readonly ILibSqlClient _client = fixture.Client;

    [Fact]
    public async Task ExecuteAsync_SimpleSelect_ReturnsRows()
    {
        StatementResult result = await _client.ExecuteAsync("SELECT 1 AS num, 'hello' AS greeting");

        Assert.Equal(2, result.Cols.Count);
        Assert.Equal("num", result.Cols[0].Name);
        Assert.Equal("greeting", result.Cols[1].Name);
        Assert.Single(result.Rows);

        Value numValue = result.Rows[0][0];
        Assert.IsType<IntegerValue>(numValue);
        Assert.Equal(1L, ((IntegerValue)numValue).Val);

        Value textValue = result.Rows[0][1];
        Assert.IsType<TextValue>(textValue);
        Assert.Equal("hello", ((TextValue)textValue).Val);
    }

    [Fact]
    public async Task ExecuteAsync_CreateTableAndInsert_AffectsRows()
    {
        string tableName = $"test_exec_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT)"
            );

            StatementResult insertResult = await _client.ExecuteAsync(
                $"INSERT INTO {tableName} (name) VALUES ('alice')"
            );
            Assert.Equal(1, insertResult.AffectedRowCount);
            Assert.NotNull(insertResult.LastInsertRowId);

            StatementResult selectResult = await _client.ExecuteAsync(
                $"SELECT id, name FROM {tableName}"
            );
            Assert.Single(selectResult.Rows);
            Assert.Equal("alice", ((TextValue)selectResult.Rows[0][1]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteAsync_PositionalParams_BindsCorrectly()
    {
        string tableName = $"test_pos_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)"
            );
            await _client.ExecuteAsync(
                $"INSERT INTO {tableName} (name, age) VALUES (?, ?)",
                [Value.Text("bob"), Value.Integer(30)]
            );

            StatementResult result = await _client.ExecuteAsync(
                $"SELECT name, age FROM {tableName} WHERE age > ?",
                [Value.Integer(25)]
            );

            Assert.Single(result.Rows);
            Assert.Equal("bob", ((TextValue)result.Rows[0][0]).Val);
            Assert.Equal(30L, ((IntegerValue)result.Rows[0][1]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteAsync_NamedParams_BindsCorrectly()
    {
        string tableName = $"test_named_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT, score REAL)"
            );
            await _client.ExecuteAsync(
                $"INSERT INTO {tableName} (name, score) VALUES (:name, :score)",
                namedArgs:
                [
                    new NamedArg { Name = ":name", Value = Value.Text("charlie") },
                    new NamedArg { Name = ":score", Value = Value.Float(95.5) },
                ]
            );

            StatementResult result = await _client.ExecuteAsync(
                $"SELECT name, score FROM {tableName} WHERE name = :name",
                namedArgs: [new NamedArg { Name = ":name", Value = Value.Text("charlie") }]
            );

            Assert.Single(result.Rows);
            Assert.Equal("charlie", ((TextValue)result.Rows[0][0]).Val);
            Assert.Equal(95.5, ((FloatValue)result.Rows[0][1]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteAsync_NullValue_HandledCorrectly()
    {
        string tableName = $"test_null_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, data TEXT)"
            );
            await _client.ExecuteAsync(
                $"INSERT INTO {tableName} (data) VALUES (?)",
                [Value.Null()]
            );

            StatementResult result = await _client.ExecuteAsync($"SELECT data FROM {tableName}");
            Assert.Single(result.Rows);
            Assert.IsType<NullValue>(result.Rows[0][0]);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteAsync_BlobValue_HandledCorrectly()
    {
        string tableName = $"test_blob_{Guid.NewGuid():N}";
        try
        {
            byte[] data = [0x01, 0x02, 0x03, 0xFF];
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, data BLOB)"
            );
            await _client.ExecuteAsync(
                $"INSERT INTO {tableName} (data) VALUES (?)",
                [Value.Blob(data)]
            );

            StatementResult result = await _client.ExecuteAsync($"SELECT data FROM {tableName}");
            Assert.Single(result.Rows);
            BlobValue blobValue = Assert.IsType<BlobValue>(result.Rows[0][0]);
            Assert.Equal(data, blobValue.Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteAsync_UpdateAndDelete_AffectsCorrectRows()
    {
        string tableName = $"test_upd_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, val INTEGER)"
            );
            await _client.ExecuteAsync($"INSERT INTO {tableName} (val) VALUES (1), (2), (3)");

            StatementResult updateResult = await _client.ExecuteAsync(
                $"UPDATE {tableName} SET val = val + 10 WHERE val > 1"
            );
            Assert.Equal(2, updateResult.AffectedRowCount);

            StatementResult deleteResult = await _client.ExecuteAsync(
                $"DELETE FROM {tableName} WHERE val = 1"
            );
            Assert.Equal(1, deleteResult.AffectedRowCount);

            StatementResult selectResult = await _client.ExecuteAsync(
                $"SELECT val FROM {tableName} ORDER BY val"
            );
            Assert.Equal(2, selectResult.Rows.Count);
            Assert.Equal(12L, ((IntegerValue)selectResult.Rows[0][0]).Val);
            Assert.Equal(13L, ((IntegerValue)selectResult.Rows[1][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteAsync_InvalidSql_ThrowsLibSqlException()
    {
        await Assert.ThrowsAsync<LibSqlException>(() =>
            _client.ExecuteAsync("SELECT FROM WHERE INVALID")
        );
    }
}
