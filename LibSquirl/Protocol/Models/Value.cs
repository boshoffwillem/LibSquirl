using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibSquirl.Protocol.Models;

[JsonConverter(typeof(ValueConverter))]
public abstract record Value
{
    public abstract string Type { get; }

    public static Value Null() => NullValue.Instance;
    public static Value Integer(long value) => new IntegerValue(value);
    public static Value Float(double value) => new FloatValue(value);
    public static Value Text(string value) => new TextValue(value);
    public static Value Blob(byte[] value) => new BlobValue(value);
}

public sealed record NullValue : Value
{
    public static readonly NullValue Instance = new();

    public override string Type => "null";
}

public sealed record IntegerValue(long Val) : Value
{
    public override string Type => "integer";
}

public sealed record FloatValue(double Val) : Value
{
    public override string Type => "float";
}

public sealed record TextValue(string Val) : Value
{
    public override string Type => "text";
}

public sealed record BlobValue(byte[] Val) : Value
{
    public override string Type => "blob";
}

internal sealed class ValueConverter : JsonConverter<Value>
{
    public override Value Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject");
        }

        string? type = null;
        string? stringValue = null;
        double? numberValue = null;
        string? base64Value = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName");
            }

            string propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName)
            {
                case "type":
                    type = reader.GetString();
                    break;
                case "value":
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        stringValue = reader.GetString();
                    }
                    else if (reader.TokenType == JsonTokenType.Number)
                    {
                        numberValue = reader.GetDouble();
                    }
                    break;
                case "base64":
                    base64Value = reader.GetString();
                    break;
            }
        }

        return type switch
        {
            "null" => NullValue.Instance,
            "integer" => new IntegerValue(long.Parse(stringValue!)),
            "float" => new FloatValue(numberValue ?? double.Parse(stringValue!)),
            "text" => new TextValue(stringValue!),
            "blob" => new BlobValue(Convert.FromBase64String(PadBase64(base64Value!))),
            _ => throw new JsonException($"Unknown value type: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Value value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case NullValue:
                writer.WriteString("type", "null");
                break;
            case IntegerValue iv:
                writer.WriteString("type", "integer");
                writer.WriteString("value", iv.Val.ToString());
                break;
            case FloatValue fv:
                writer.WriteString("type", "float");
                writer.WriteNumber("value", fv.Val);
                break;
            case TextValue tv:
                writer.WriteString("type", "text");
                writer.WriteString("value", tv.Val);
                break;
            case BlobValue bv:
                writer.WriteString("type", "blob");
                writer.WriteString("base64", Convert.ToBase64String(bv.Val));
                break;
        }

        writer.WriteEndObject();
    }

    private static string PadBase64(string base64)
    {
        int remainder = base64.Length % 4;
        return remainder switch
        {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => base64
        };
    }
}
