using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

public sealed class Statement
{
    [JsonPropertyName("sql")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sql { get; set; }

    [JsonPropertyName("sql_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SqlId { get; set; }

    [JsonPropertyName("args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Value>? Args { get; set; }

    [JsonPropertyName("named_args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<NamedArg>? NamedArgs { get; set; }

    [JsonPropertyName("want_rows")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? WantRows { get; set; }
}

public sealed class NamedArg
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    public required Value Value { get; set; }
}
