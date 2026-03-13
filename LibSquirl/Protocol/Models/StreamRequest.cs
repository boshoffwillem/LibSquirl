using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

[JsonConverter(typeof(StreamRequestConverter))]
public abstract class StreamRequest
{
    public abstract string Type { get; }

    public static StreamRequest Execute(Statement stmt)
    {
        return new ExecuteStreamRequest { Stmt = stmt };
    }

    public static StreamRequest Close()
    {
        return new CloseStreamRequest();
    }

    public static StreamRequest ExecuteBatch(Batch batch)
    {
        return new BatchStreamRequest { Batch = batch };
    }

    public static StreamRequest Sequence(string? sql = null, int? sqlId = null)
    {
        return new SequenceStreamRequest { Sql = sql, SqlId = sqlId };
    }

    public static StreamRequest Describe(string? sql = null, int? sqlId = null)
    {
        return new DescribeStreamRequest { Sql = sql, SqlId = sqlId };
    }

    public static StreamRequest StoreSql(int sqlId, string sql)
    {
        return new StoreSqlStreamRequest { SqlId = sqlId, Sql = sql };
    }

    public static StreamRequest CloseSql(int sqlId)
    {
        return new CloseSqlStreamRequest { SqlId = sqlId };
    }
}

public sealed class ExecuteStreamRequest : StreamRequest
{
    public override string Type => "execute";
    public required Statement Stmt { get; set; }
}

public sealed class CloseStreamRequest : StreamRequest
{
    public override string Type => "close";
}

public sealed class BatchStreamRequest : StreamRequest
{
    public override string Type => "batch";
    public required Batch Batch { get; set; }
}

public sealed class SequenceStreamRequest : StreamRequest
{
    public override string Type => "sequence";
    public string? Sql { get; set; }
    public int? SqlId { get; set; }
}

public sealed class DescribeStreamRequest : StreamRequest
{
    public override string Type => "describe";
    public string? Sql { get; set; }
    public int? SqlId { get; set; }
}

public sealed class StoreSqlStreamRequest : StreamRequest
{
    public override string Type => "store_sql";
    public required int SqlId { get; set; }
    public required string Sql { get; set; }
}

public sealed class CloseSqlStreamRequest : StreamRequest
{
    public override string Type => "close_sql";
    public required int SqlId { get; set; }
}

internal sealed class StreamRequestConverter : JsonConverter<StreamRequest>
{
    public override StreamRequest Read(
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
            "execute" => new ExecuteStreamRequest
            {
                Stmt = JsonSerializer.Deserialize<Statement>(
                    root.GetProperty("stmt").GetRawText(),
                    options
                )!,
            },
            "close" => new CloseStreamRequest(),
            "batch" => new BatchStreamRequest
            {
                Batch = JsonSerializer.Deserialize<Batch>(
                    root.GetProperty("batch").GetRawText(),
                    options
                )!,
            },
            "sequence" => new SequenceStreamRequest
            {
                Sql = root.TryGetProperty("sql", out JsonElement seqSql)
                    ? seqSql.GetString()
                    : null,
                SqlId = root.TryGetProperty("sql_id", out JsonElement seqSqlId)
                    ? seqSqlId.GetInt32()
                    : null,
            },
            "describe" => new DescribeStreamRequest
            {
                Sql = root.TryGetProperty("sql", out JsonElement descSql)
                    ? descSql.GetString()
                    : null,
                SqlId = root.TryGetProperty("sql_id", out JsonElement descSqlId)
                    ? descSqlId.GetInt32()
                    : null,
            },
            "store_sql" => new StoreSqlStreamRequest
            {
                SqlId = root.GetProperty("sql_id").GetInt32(),
                Sql = root.GetProperty("sql").GetString()!,
            },
            "close_sql" => new CloseSqlStreamRequest
            {
                SqlId = root.GetProperty("sql_id").GetInt32(),
            },
            _ => throw new JsonException($"Unknown stream request type: {type}"),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        StreamRequest value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);

        switch (value)
        {
            case ExecuteStreamRequest exec:
                writer.WritePropertyName("stmt");
                JsonSerializer.Serialize(writer, exec.Stmt, options);
                break;
            case BatchStreamRequest batch:
                writer.WritePropertyName("batch");
                JsonSerializer.Serialize(writer, batch.Batch, options);
                break;
            case SequenceStreamRequest seq:
                if (seq.Sql is not null)
                {
                    writer.WriteString("sql", seq.Sql);
                }

                if (seq.SqlId is not null)
                {
                    writer.WriteNumber("sql_id", seq.SqlId.Value);
                }

                break;
            case DescribeStreamRequest desc:
                if (desc.Sql is not null)
                {
                    writer.WriteString("sql", desc.Sql);
                }

                if (desc.SqlId is not null)
                {
                    writer.WriteNumber("sql_id", desc.SqlId.Value);
                }

                break;
            case StoreSqlStreamRequest store:
                writer.WriteNumber("sql_id", store.SqlId);
                writer.WriteString("sql", store.Sql);
                break;
            case CloseSqlStreamRequest close:
                writer.WriteNumber("sql_id", close.SqlId);
                break;
            case CloseStreamRequest:
                break;
        }

        writer.WriteEndObject();
    }
}
