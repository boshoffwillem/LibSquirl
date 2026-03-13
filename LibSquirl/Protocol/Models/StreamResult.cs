using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

public sealed class StreamResult
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StreamResponse? Response { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LibSqlError? Error { get; set; }

    public bool IsOk => Type == "ok";
    public bool IsError => Type == "error";
}

[JsonConverter(typeof(StreamResponseConverter))]
public abstract class StreamResponse
{
    public abstract string Type { get; }
}

public sealed class ExecuteStreamResponse : StreamResponse
{
    public override string Type => "execute";
    public required StatementResult Result { get; set; }
}

public sealed class CloseStreamResponse : StreamResponse
{
    public override string Type => "close";
}

public sealed class BatchStreamResponse : StreamResponse
{
    public override string Type => "batch";
    public required BatchResult Result { get; set; }
}

public sealed class SequenceStreamResponse : StreamResponse
{
    public override string Type => "sequence";
}

public sealed class DescribeStreamResponse : StreamResponse
{
    public override string Type => "describe";
    public required DescribeResult Result { get; set; }
}

public sealed class StoreSqlStreamResponse : StreamResponse
{
    public override string Type => "store_sql";
}

public sealed class CloseSqlStreamResponse : StreamResponse
{
    public override string Type => "close_sql";
}

internal sealed class StreamResponseConverter : JsonConverter<StreamResponse>
{
    public override StreamResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        string type = root.GetProperty("type").GetString()!;

        return type switch
        {
            "execute" => new ExecuteStreamResponse
            {
                Result = JsonSerializer.Deserialize<StatementResult>(
                    root.GetProperty("result").GetRawText(),
                    options
                )!,
            },
            "close" => new CloseStreamResponse(),
            "batch" => new BatchStreamResponse
            {
                Result = JsonSerializer.Deserialize<BatchResult>(
                    root.GetProperty("result").GetRawText(),
                    options
                )!,
            },
            "sequence" => new SequenceStreamResponse(),
            "describe" => new DescribeStreamResponse
            {
                Result = JsonSerializer.Deserialize<DescribeResult>(
                    root.GetProperty("result").GetRawText(),
                    options
                )!,
            },
            "store_sql" => new StoreSqlStreamResponse(),
            "close_sql" => new CloseSqlStreamResponse(),
            _ => throw new JsonException($"Unknown stream response type: {type}"),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        StreamResponse value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);

        switch (value)
        {
            case ExecuteStreamResponse exec:
                writer.WritePropertyName("result");
                JsonSerializer.Serialize(writer, exec.Result, options);
                break;
            case BatchStreamResponse batch:
                writer.WritePropertyName("result");
                JsonSerializer.Serialize(writer, batch.Result, options);
                break;
            case DescribeStreamResponse desc:
                writer.WritePropertyName("result");
                JsonSerializer.Serialize(writer, desc.Result, options);
                break;
        }

        writer.WriteEndObject();
    }
}
