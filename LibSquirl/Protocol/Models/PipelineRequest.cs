using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

public sealed class PipelineRequest
{
    [JsonPropertyName("baton")]
    public string? Baton { get; set; }

    [JsonPropertyName("requests")]
    public required List<StreamRequest> Requests { get; set; }
}

public sealed class PipelineResponse
{
    [JsonPropertyName("baton")]
    public string? Baton { get; set; }

    [JsonPropertyName("base_url")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("results")]
    public List<StreamResult> Results { get; set; } = [];
}
