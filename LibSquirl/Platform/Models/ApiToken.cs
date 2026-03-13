using System.Text.Json.Serialization;

namespace LibSquirl.Platform.Models;

public sealed class ApiToken
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

public sealed class TokenResponse
{
    [JsonPropertyName("jwt")]
    public string Jwt { get; set; } = string.Empty;
}

public sealed class CreateTokenRequest
{
    [JsonPropertyName("permissions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TokenPermissions? Permissions { get; set; }
}

public sealed class TokenPermissions
{
    [JsonPropertyName("read_attach")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TokenReadAttach? ReadAttach { get; set; }
}

public sealed class TokenReadAttach
{
    [JsonPropertyName("databases")]
    public List<string> Databases { get; set; } = [];
}
