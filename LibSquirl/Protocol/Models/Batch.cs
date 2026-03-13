using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

public sealed class Batch
{
    [JsonPropertyName("steps")]
    public required List<BatchStep> Steps { get; set; }
}

public sealed class BatchStep
{
    [JsonPropertyName("condition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BatchCondition? Condition { get; set; }

    [JsonPropertyName("stmt")]
    public required Statement Stmt { get; set; }
}

public sealed class BatchResult
{
    [JsonPropertyName("step_results")]
    public List<StatementResult?> StepResults { get; set; } = [];

    [JsonPropertyName("step_errors")]
    public List<LibSqlError?> StepErrors { get; set; } = [];
}
