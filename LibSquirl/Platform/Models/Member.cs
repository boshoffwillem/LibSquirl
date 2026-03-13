using System.Text.Json.Serialization;

namespace LibSquirl.Platform.Models;

public sealed class Member
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public sealed class AddMemberRequest
{
    [JsonPropertyName("username")]
    public required string Username { get; set; }

    [JsonPropertyName("role")]
    public required string Role { get; set; }
}

public sealed class UpdateMemberRequest
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }
}
