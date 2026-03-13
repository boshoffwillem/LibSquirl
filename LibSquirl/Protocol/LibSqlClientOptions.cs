namespace LibSquirl.Protocol;

public sealed class LibSqlClientOptions
{
    public required string Url { get; set; }
    public string? AuthToken { get; set; }
}
