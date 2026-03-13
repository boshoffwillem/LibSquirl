using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

public sealed class DescribeResult
{
    [JsonPropertyName("params")]
    public List<DescribeParam> Params { get; set; } = [];

    [JsonPropertyName("cols")]
    public List<DescribeColumn> Cols { get; set; } = [];

    [JsonPropertyName("is_explain")]
    public bool IsExplain { get; set; }

    [JsonPropertyName("is_readonly")]
    public bool IsReadonly { get; set; }
}

public sealed class DescribeParam
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class DescribeColumn
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("decltype")]
    public string? DeclType { get; set; }
}
