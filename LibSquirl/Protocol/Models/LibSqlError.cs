using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

public sealed class LibSqlError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; set; }
}
