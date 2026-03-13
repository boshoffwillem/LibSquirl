namespace LibSquirl.Platform;

public sealed class TursoPlatformOptions
{
    public string BaseUrl { get; set; } = "https://api.turso.tech";
    public required string ApiToken { get; set; }
    public required string OrganizationSlug { get; set; }
}
