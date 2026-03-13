using System.Text.Json.Serialization;

namespace LibSquirl.Platform.Models;

public sealed class Database
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("DbId")]
    public string DbId { get; set; } = string.Empty;

    [JsonPropertyName("Hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("block_reads")]
    public bool BlockReads { get; set; }

    [JsonPropertyName("block_writes")]
    public bool BlockWrites { get; set; }

    [JsonPropertyName("regions")]
    public List<string> Regions { get; set; } = [];

    [JsonPropertyName("primaryRegion")]
    public string PrimaryRegion { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("delete_protection")]
    public bool DeleteProtection { get; set; }

    [JsonPropertyName("parent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DatabaseParent? Parent { get; set; }
}

public sealed class DatabaseParent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("branched_at")]
    public string? BranchedAt { get; set; }
}

public sealed class CreateDatabaseRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("group")]
    public required string Group { get; set; }

    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DatabaseSeed? Seed { get; set; }

    [JsonPropertyName("size_limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SizeLimit { get; set; }
}

public sealed class DatabaseSeed
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    [JsonPropertyName("timestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Timestamp { get; set; }
}

public sealed class DatabaseInstance
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;
}

public sealed class DatabaseStats
{
    [JsonPropertyName("rows_read")]
    public long RowsRead { get; set; }

    [JsonPropertyName("rows_written")]
    public long RowsWritten { get; set; }

    [JsonPropertyName("storage_bytes")]
    public long StorageBytes { get; set; }
}
