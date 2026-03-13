using System.Text.Json.Serialization;

namespace LibSquirl.Platform.Models;

public sealed class GroupConfiguration
{
    [JsonPropertyName("heartbeat_url")]
    public string? HeartbeatUrl { get; set; }

    [JsonPropertyName("allow_attach")]
    public bool? AllowAttach { get; set; }
}

public sealed class UpdateGroupConfigurationRequest
{
    [JsonPropertyName("heartbeat_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HeartbeatUrl { get; set; }

    [JsonPropertyName("allow_attach")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowAttach { get; set; }
}

public sealed class TransferGroupRequest
{
    [JsonPropertyName("organization")]
    public required string Organization { get; set; }
}
