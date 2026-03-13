using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

public sealed class MigrationJobsSummary
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("migrations")]
    public List<MigrationJob> Migrations { get; set; } = [];
}

public sealed class MigrationJob
{
    [JsonPropertyName("job_id")]
    public int JobId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public sealed class MigrationJobDetail
{
    [JsonPropertyName("job_id")]
    public int JobId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("progress")]
    public List<MigrationProgress> Progress { get; set; } = [];
}

public sealed class MigrationProgress
{
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
