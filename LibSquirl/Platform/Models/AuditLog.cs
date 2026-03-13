using System.Text.Json.Serialization;

namespace LibSquirl.Platform.Models;

public sealed class AuditLog
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("origin")]
    public string Origin { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}

public sealed class AuditLogPagination
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_rows")]
    public int TotalRows { get; set; }
}

public sealed class AuditLogsResponse
{
    [JsonPropertyName("audit_logs")]
    public List<AuditLog> AuditLogs { get; set; } = [];

    [JsonPropertyName("pagination")]
    public AuditLogPagination Pagination { get; set; } = new();
}
