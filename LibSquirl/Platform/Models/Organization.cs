using System.Text.Json.Serialization;

namespace LibSquirl.Platform.Models;

public sealed class Organization
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("overages")]
    public bool Overages { get; set; }

    [JsonPropertyName("blocked_reads")]
    public bool BlockedReads { get; set; }

    [JsonPropertyName("blocked_writes")]
    public bool BlockedWrites { get; set; }

    [JsonPropertyName("plan_id")]
    public string? PlanId { get; set; }

    [JsonPropertyName("plan_timeline")]
    public string? PlanTimeline { get; set; }

    [JsonPropertyName("platform")]
    public string? Platform { get; set; }
}
