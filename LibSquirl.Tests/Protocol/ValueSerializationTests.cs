using System.Text.Json;

using LibSquirl.Protocol.Models;

namespace LibSquirl.Tests.Protocol;

public class ValueSerializationTests
{
    private static readonly JsonSerializerOptions s_options = new();

    [Fact]
    public void NullValue_RoundTrips()
    {
        Value original = Value.Null();
        string json = JsonSerializer.Serialize(original, s_options);
        Value? deserialized = JsonSerializer.Deserialize<Value>(json, s_options);

        Assert.IsType<NullValue>(deserialized);
        Assert.Contains("\"type\":\"null\"", json);
    }

    [Fact]
    public void IntegerValue_RoundTrips()
    {
        Value original = Value.Integer(42);
        string json = JsonSerializer.Serialize(original, s_options);
        Value? deserialized = JsonSerializer.Deserialize<Value>(json, s_options);

        IntegerValue iv = Assert.IsType<IntegerValue>(deserialized);
        Assert.Equal(42L, iv.Val);
        Assert.Contains("\"type\":\"integer\"", json);
        Assert.Contains("\"value\":\"42\"", json);
    }

    [Fact]
    public void IntegerValue_LargeNumber_PreservesPrecision()
    {
        long largeNum = long.MaxValue;
        Value original = Value.Integer(largeNum);
        string json = JsonSerializer.Serialize(original, s_options);
        Value? deserialized = JsonSerializer.Deserialize<Value>(json, s_options);

        IntegerValue iv = Assert.IsType<IntegerValue>(deserialized);
        Assert.Equal(largeNum, iv.Val);
    }

    [Fact]
    public void FloatValue_RoundTrips()
    {
        Value original = Value.Float(3.14);
        string json = JsonSerializer.Serialize(original, s_options);
        Value? deserialized = JsonSerializer.Deserialize<Value>(json, s_options);

        FloatValue fv = Assert.IsType<FloatValue>(deserialized);
        Assert.Equal(3.14, fv.Val, precision: 10);
    }

    [Fact]
    public void TextValue_RoundTrips()
    {
        Value original = Value.Text("hello world");
        string json = JsonSerializer.Serialize(original, s_options);
        Value? deserialized = JsonSerializer.Deserialize<Value>(json, s_options);

        TextValue tv = Assert.IsType<TextValue>(deserialized);
        Assert.Equal("hello world", tv.Val);
    }

    [Fact]
    public void BlobValue_RoundTrips()
    {
        byte[] data = [0x01, 0x02, 0x03, 0xFF];
        Value original = Value.Blob(data);
        string json = JsonSerializer.Serialize(original, s_options);
        Value? deserialized = JsonSerializer.Deserialize<Value>(json, s_options);

        BlobValue bv = Assert.IsType<BlobValue>(deserialized);
        Assert.Equal(data, bv.Val);
        Assert.Contains("\"base64\"", json);
    }

    [Fact]
    public void BatchCondition_Ok_RoundTrips()
    {
        BatchCondition original = BatchCondition.Ok(0);
        string json = JsonSerializer.Serialize(original, s_options);
        BatchCondition? deserialized = JsonSerializer.Deserialize<BatchCondition>(json, s_options);

        OkCondition ok = Assert.IsType<OkCondition>(deserialized);
        Assert.Equal(0, ok.Step);
    }

    [Fact]
    public void BatchCondition_Complex_RoundTrips()
    {
        BatchCondition original = BatchCondition.And(
            BatchCondition.Ok(0),
            BatchCondition.Not(BatchCondition.Error(1)));

        string json = JsonSerializer.Serialize(original, s_options);
        BatchCondition? deserialized = JsonSerializer.Deserialize<BatchCondition>(json, s_options);

        AndCondition and = Assert.IsType<AndCondition>(deserialized);
        Assert.Equal(2, and.Conds.Count);
        Assert.IsType<OkCondition>(and.Conds[0]);
        NotCondition not = Assert.IsType<NotCondition>(and.Conds[1]);
        Assert.IsType<ErrorCondition>(not.Cond);
    }

    [Fact]
    public void StreamRequest_Execute_Serializes()
    {
        StreamRequest request = StreamRequest.Execute(new Statement { Sql = "SELECT 1" });
        string json = JsonSerializer.Serialize(request, s_options);

        Assert.Contains("\"type\":\"execute\"", json);
        Assert.Contains("\"sql\":\"SELECT 1\"", json);
    }

    [Fact]
    public void StreamRequest_Close_Serializes()
    {
        StreamRequest request = StreamRequest.Close();
        string json = JsonSerializer.Serialize(request, s_options);

        Assert.Contains("\"type\":\"close\"", json);
    }

    [Fact]
    public void StreamRequest_StoreSql_Serializes()
    {
        StreamRequest request = StreamRequest.StoreSql(1, "SELECT 1");
        string json = JsonSerializer.Serialize(request, s_options);

        Assert.Contains("\"type\":\"store_sql\"", json);
        Assert.Contains("\"sql_id\":1", json);
    }

    [Fact]
    public void PipelineRequest_Serializes()
    {
        PipelineRequest request = new()
        {
            Requests =
            [
                StreamRequest.Execute(new Statement { Sql = "SELECT 1" }),
                StreamRequest.Close()
            ]
        };

        string json = JsonSerializer.Serialize(request, s_options);
        Assert.Contains("\"requests\"", json);
        Assert.Contains("\"execute\"", json);
        Assert.Contains("\"close\"", json);
    }
}
