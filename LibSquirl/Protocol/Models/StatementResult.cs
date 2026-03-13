using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

public sealed class StatementResult
{
    [JsonPropertyName("cols")]
    public List<Column> Cols { get; set; } = [];

    [JsonPropertyName("rows")]
    public List<List<Value>> Rows { get; set; } = [];

    [JsonPropertyName("affected_row_count")]
    public int AffectedRowCount { get; set; }

    [JsonPropertyName("last_insert_rowid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastInsertRowId { get; set; }

    [JsonPropertyName("replication_index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReplicationIndex { get; set; }

    [JsonPropertyName("rows_read")]
    public int RowsRead { get; set; }

    [JsonPropertyName("rows_written")]
    public int RowsWritten { get; set; }

    [JsonPropertyName("query_duration_ms")]
    public double QueryDurationMs { get; set; }
}

public sealed class Column
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("decltype")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeclType { get; set; }
}