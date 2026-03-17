using LibSquirl.Protocol;
using LibSquirl.Protocol.Models;

namespace LibSquirl.Tests.Protocol;

[Collection("LibSqlServer")]
public class LibSqlClientExtensionsTests(LibSqlServerFixture fixture)
{
    private readonly ILibSqlClient _client = fixture.Client;

    [Fact]
    public async Task ExecuteMultipleAsync_EmptyList_ReturnsEmptyList()
    {
        List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements = [];

        IReadOnlyList<StatementResult> results = await _client.ExecuteMultipleAsync(statements);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ExecuteMultipleAsync_SingleStatement_ReturnsOneResult()
    {
        List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements =
        [
            ("SELECT 1 AS num", null),
        ];

        IReadOnlyList<StatementResult> results = await _client.ExecuteMultipleAsync(statements);

        Assert.Single(results);
        Assert.Equal(1L, ((IntegerValue)results[0].Rows[0][0]).Val);
    }

    [Fact]
    public async Task ExecuteMultipleAsync_MultipleStatements_ReturnsAllResults()
    {
        List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements =
        [
            ("SELECT 1 AS a", null),
            ("SELECT 2 AS b", null),
            ("SELECT 3 AS c", null),
        ];

        IReadOnlyList<StatementResult> results = await _client.ExecuteMultipleAsync(statements);

        Assert.Equal(3, results.Count);
        Assert.Equal(1L, ((IntegerValue)results[0].Rows[0][0]).Val);
        Assert.Equal(2L, ((IntegerValue)results[1].Rows[0][0]).Val);
        Assert.Equal(3L, ((IntegerValue)results[2].Rows[0][0]).Val);
    }

    [Fact]
    public async Task ExecuteMultipleAsync_WithNamedArgs_BindsCorrectly()
    {
        string tableName = $"test_exm_args_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT, score REAL)"
            );

            List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements =
            [
                (
                    $"INSERT INTO {tableName} (name, score) VALUES (:name, :score)",
                    [Sql.Arg(":name", "alice"), Sql.Arg(":score", 95.5)]
                ),
                (
                    $"INSERT INTO {tableName} (name, score) VALUES (:name, :score)",
                    [Sql.Arg(":name", "bob"), Sql.Arg(":score", 88.0)]
                ),
                ($"SELECT name, score FROM {tableName} ORDER BY name", null),
            ];

            IReadOnlyList<StatementResult> results = await _client.ExecuteMultipleAsync(statements);

            Assert.Equal(3, results.Count);
            Assert.Equal(1, results[0].AffectedRowCount);
            Assert.Equal(1, results[1].AffectedRowCount);
            Assert.Equal(2, results[2].Rows.Count);
            Assert.Equal("alice", ((TextValue)results[2].Rows[0][0]).Val);
            Assert.Equal("bob", ((TextValue)results[2].Rows[1][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteMultipleAsync_WithSqlArgHelpers_BindsAllTypes()
    {
        string tableName = $"test_exm_types_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id TEXT, name TEXT, active INTEGER, score REAL, amount TEXT)"
            );

            Guid id = Guid.NewGuid();
            List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements =
            [
                (
                    $"INSERT INTO {tableName} (id, name, active, score, amount) VALUES (:id, :name, :active, :score, :amount)",
                    [
                        Sql.Arg(":id", id),
                        Sql.Arg(":name", "test"),
                        Sql.Arg(":active", true),
                        Sql.Arg(":score", 3.14),
                        Sql.Arg(":amount", 99.9m),
                    ]
                ),
                ($"SELECT * FROM {tableName}", null),
            ];

            IReadOnlyList<StatementResult> results = await _client.ExecuteMultipleAsync(statements);

            Assert.Equal(2, results.Count);
            List<Value> row = results[1].Rows[0];
            Assert.Equal(id.ToString(), ((TextValue)row[0]).Val);
            Assert.Equal("test", ((TextValue)row[1]).Val);
            Assert.Equal(1L, ((IntegerValue)row[2]).Val);
            Assert.Equal(3.14, ((FloatValue)row[3]).Val);
            Assert.Equal("99.90", ((TextValue)row[4]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteMultipleAsync_StepError_ThrowsLibSqlException()
    {
        string tableName = $"test_exm_err_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT NOT NULL)"
            );

            List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements =
            [
                ($"INSERT INTO {tableName} (name) VALUES ('ok')", null),
                ($"INSERT INTO {tableName} (name) VALUES (NULL)", null), // NOT NULL violation
            ];

            LibSqlException ex = await Assert.ThrowsAsync<LibSqlException>(() =>
                _client.ExecuteMultipleAsync(statements)
            );

            Assert.Contains("step 1", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteMultipleAsync_MixedReadsAndWrites_WorksCorrectly()
    {
        string tableName = $"test_exm_mix_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, val TEXT)"
            );
            await _client.ExecuteAsync($"INSERT INTO {tableName} (val) VALUES ('existing')");

            List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements =
            [
                ($"SELECT COUNT(*) FROM {tableName}", null),
                ($"INSERT INTO {tableName} (val) VALUES ('new')", null),
                ($"SELECT val FROM {tableName} ORDER BY val", null),
            ];

            IReadOnlyList<StatementResult> results = await _client.ExecuteMultipleAsync(statements);

            Assert.Equal(3, results.Count);
            // Count before insert
            Assert.Equal(1L, ((IntegerValue)results[0].Rows[0][0]).Val);
            // Insert result
            Assert.Equal(1, results[1].AffectedRowCount);
            // Select after insert includes both rows
            Assert.Equal(2, results[2].Rows.Count);
            Assert.Equal("existing", ((TextValue)results[2].Rows[0][0]).Val);
            Assert.Equal("new", ((TextValue)results[2].Rows[1][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteMultipleAsync_ResultsCanBeMappedToObjects()
    {
        string tableName = $"test_exm_map_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)"
            );

            List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements =
            [
                (
                    $"INSERT INTO {tableName} (name, age) VALUES (:name, :age)",
                    [Sql.Arg(":name", "alice"), Sql.Arg(":age", 30)]
                ),
                (
                    $"INSERT INTO {tableName} (name, age) VALUES (:name, :age)",
                    [Sql.Arg(":name", "bob"), Sql.Arg(":age", 25)]
                ),
                ($"SELECT id, name, age FROM {tableName} ORDER BY name", null),
            ];

            IReadOnlyList<StatementResult> results = await _client.ExecuteMultipleAsync(statements);

            List<PersonRow> people = results[2].MapTo<PersonRow>();
            Assert.Equal(2, people.Count);
            Assert.Equal("alice", people[0].Name);
            Assert.Equal(30, people[0].Age);
            Assert.Equal("bob", people[1].Name);
            Assert.Equal(25, people[1].Age);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteMultipleAsync_WithInClause_FiltersCorrectly()
    {
        string tableName = $"test_exm_in_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id TEXT PRIMARY KEY, name TEXT)"
            );

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();
            Guid id3 = Guid.NewGuid();

            // Insert 3 rows
            List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> insertStatements =
            [
                (
                    $"INSERT INTO {tableName} (id, name) VALUES (:id, :name)",
                    [Sql.Arg(":id", id1), Sql.Arg(":name", "alice")]
                ),
                (
                    $"INSERT INTO {tableName} (id, name) VALUES (:id, :name)",
                    [Sql.Arg(":id", id2), Sql.Arg(":name", "bob")]
                ),
                (
                    $"INSERT INTO {tableName} (id, name) VALUES (:id, :name)",
                    [Sql.Arg(":id", id3), Sql.Arg(":name", "charlie")]
                ),
            ];
            await _client.ExecuteMultipleAsync(insertStatements);

            // Query with IN clause for 2 of the 3 IDs
            (string inSql, NamedArg[] inArgs) = Sql.In(":id", new List<Guid> { id1, id3 });
            List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> selectStatements =
            [
                ($"SELECT name FROM {tableName} WHERE id IN ({inSql}) ORDER BY name", inArgs),
            ];

            IReadOnlyList<StatementResult> results = await _client.ExecuteMultipleAsync(
                selectStatements
            );

            Assert.Equal(2, results[0].Rows.Count);
            Assert.Equal("alice", ((TextValue)results[0].Rows[0][0]).Val);
            Assert.Equal("charlie", ((TextValue)results[0].Rows[1][0]).Val);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    [Fact]
    public async Task ExecuteMultipleAsync_NullableArgs_HandledCorrectly()
    {
        string tableName = $"test_exm_null_{Guid.NewGuid():N}";
        try
        {
            await _client.ExecuteAsync(
                $"CREATE TABLE {tableName} (id INTEGER PRIMARY KEY, label TEXT, ref_id TEXT)"
            );

            List<(string Sql, IReadOnlyList<NamedArg>? NamedArgs)> statements =
            [
                (
                    $"INSERT INTO {tableName} (label, ref_id) VALUES (:label, :ref)",
                    [
                        Sql.ArgNullable(":label", (string?)"has-value"),
                        Sql.ArgNullable(":ref", (Guid?)null),
                    ]
                ),
                ($"SELECT label, ref_id FROM {tableName}", null),
            ];

            IReadOnlyList<StatementResult> results = await _client.ExecuteMultipleAsync(statements);

            Assert.Equal("has-value", ((TextValue)results[1].Rows[0][0]).Val);
            Assert.IsType<NullValue>(results[1].Rows[0][1]);
        }
        finally
        {
            await _client.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    // ── Test helper types ───────────────────────────────────────────────

    public sealed class PersonRow
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
