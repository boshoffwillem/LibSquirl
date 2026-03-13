using System.Text.Json.Serialization;

namespace LibSquirl.Platform.Models;

public sealed class DatabaseConfiguration
{
    [JsonPropertyName("size_limit")]
    public string? SizeLimit { get; set; }

    [JsonPropertyName("allow_attach")]
    public bool? AllowAttach { get; set; }

    [JsonPropertyName("block_reads")]
    public bool? BlockReads { get; set; }

    [JsonPropertyName("block_writes")]
    public bool? BlockWrites { get; set; }
}

public sealed class UpdateDatabaseConfigurationRequest
{
    [JsonPropertyName("size_limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SizeLimit { get; set; }

    [JsonPropertyName("allow_attach")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowAttach { get; set; }

    [JsonPropertyName("block_reads")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BlockReads { get; set; }

    [JsonPropertyName("block_writes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BlockWrites { get; set; }
}

public sealed class DatabaseUsage
{
    [JsonPropertyName("rows_read")]
    public long RowsRead { get; set; }

    [JsonPropertyName("rows_written")]
    public long RowsWritten { get; set; }

    [JsonPropertyName("storage_bytes")]
    public long StorageBytes { get; set; }

    [JsonPropertyName("bytes_synced")]
    public long BytesSynced { get; set; }
}

public sealed class ClosestRegion
{
    [JsonPropertyName("server")]
    public string Server { get; set; } = string.Empty;

    [JsonPropertyName("client")]
    public string Client { get; set; } = string.Empty;
}
