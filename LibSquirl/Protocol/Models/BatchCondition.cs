using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

[JsonConverter(typeof(BatchConditionConverter))]
public abstract record BatchCondition
{
    public abstract string Type { get; }

    public static BatchCondition Ok(int step) => new OkCondition(step);
    public static BatchCondition Error(int step) => new ErrorCondition(step);
    public static BatchCondition Not(BatchCondition cond) => new NotCondition(cond);
    public static BatchCondition And(params BatchCondition[] conds) => new AndCondition([.. conds]);
    public static BatchCondition Or(params BatchCondition[] conds) => new OrCondition([.. conds]);
}

public sealed record OkCondition(int Step) : BatchCondition
{
    public override string Type => "ok";
}

public sealed record ErrorCondition(int Step) : BatchCondition
{
    public override string Type => "error";
}

public sealed record NotCondition(BatchCondition Cond) : BatchCondition
{
    public override string Type => "not";
}

public sealed record AndCondition(List<BatchCondition> Conds) : BatchCondition
{
    public override string Type => "and";
}

public sealed record OrCondition(List<BatchCondition> Conds) : BatchCondition
{
    public override string Type => "or";
}

internal sealed class BatchConditionConverter : JsonConverter<BatchCondition>
{
    public override BatchCondition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        string type = root.GetProperty("type").GetString()!;

        return type switch
        {
            "ok" => new OkCondition(root.GetProperty("step").GetInt32()),
            "error" => new ErrorCondition(root.GetProperty("step").GetInt32()),
            "not" => new NotCondition(
                JsonSerializer.Deserialize<BatchCondition>(root.GetProperty("cond").GetRawText(), options)!),
            "and" => new AndCondition(
                JsonSerializer.Deserialize<List<BatchCondition>>(root.GetProperty("conds").GetRawText(), options)!),
            "or" => new OrCondition(
                JsonSerializer.Deserialize<List<BatchCondition>>(root.GetProperty("conds").GetRawText(), options)!),
            _ => throw new JsonException($"Unknown batch condition type: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, BatchCondition value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);

        switch (value)
        {
            case OkCondition ok:
                writer.WriteNumber("step", ok.Step);
                break;
            case ErrorCondition err:
                writer.WriteNumber("step", err.Step);
                break;
            case NotCondition not:
                writer.WritePropertyName("cond");
                JsonSerializer.Serialize(writer, not.Cond, options);
                break;
            case AndCondition and:
                writer.WritePropertyName("conds");
                JsonSerializer.Serialize(writer, and.Conds, options);
                break;
            case OrCondition or:
                writer.WritePropertyName("conds");
                JsonSerializer.Serialize(writer, or.Conds, options);
                break;
        }

        writer.WriteEndObject();
    }
}
