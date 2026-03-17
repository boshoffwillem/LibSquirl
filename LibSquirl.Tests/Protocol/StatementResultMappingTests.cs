using LibSquirl.Protocol;
using LibSquirl.Protocol.Models;

namespace LibSquirl.Tests.Protocol;

public class StatementResultMappingTests
{
    #region Float Conversions

    [Fact]
    public void MapTo_FloatValue_ConvertsToAllFloatTypes()
    {
        StatementResult result = CreateResult(
            ["doubleval", "floatval", "decimalval"],
            [
                [Value.Float(3.14), Value.Float(2.5), Value.Float(99.99)],
            ]
        );

        List<FloatTypes> items = result.MapTo<FloatTypes>();

        Assert.Single(items);
        Assert.Equal(3.14, items[0].DoubleVal);
        Assert.Equal(2.5f, items[0].FloatVal);
        Assert.Equal(99.99m, items[0].DecimalVal);
    }

    #endregion

    #region Integer-to-Enum Conversion

    [Fact]
    public void MapTo_IntegerValue_ConvertsToEnum()
    {
        StatementResult result = CreateResult(
            ["status"],
            [
                [Value.Integer(1)],
            ]
        );

        List<EnumFromInteger> items = result.MapTo<EnumFromInteger>();

        Assert.Equal(Status.Inactive, items[0].Status);
    }

    #endregion

    #region Blob Conversion

    [Fact]
    public void MapTo_BlobValue_ConvertsToByteArray()
    {
        byte[] expected = [0x01, 0x02, 0x03];
        StatementResult result = CreateResult(
            ["data"],
            [
                [Value.Blob(expected)],
            ]
        );

        List<BlobType> items = result.MapTo<BlobType>();

        Assert.Single(items);
        Assert.Equal(expected, items[0].Data);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void MapTo_UnsupportedConversion_Throws()
    {
        StatementResult result = CreateResult(
            ["data"],
            [
                [Value.Text("not bytes")],
            ]
        );

        Assert.Throws<InvalidOperationException>(() => result.MapTo<BlobType>());
    }

    #endregion

    #region Helpers

    private static StatementResult CreateResult(string[] columnNames, List<Value>[] rows)
    {
        List<Column> cols = columnNames.Select(name => new Column { Name = name }).ToList();

        return new StatementResult { Cols = cols, Rows = rows.ToList() };
    }

    #endregion

    #region Test Types

    public sealed class SimpleUser
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public sealed class UserWithColumnName
    {
        [ColumnName("user_id")]
        public long Id { get; set; }

        [ColumnName("full_name")]
        public string Name { get; set; } = string.Empty;
    }

    public sealed class AllIntegerTypes
    {
        public long LongVal { get; set; }
        public int IntVal { get; set; }
        public short ShortVal { get; set; }
        public byte ByteVal { get; set; }
        public bool BoolVal { get; set; }
        public uint UintVal { get; set; }
    }

    public sealed class NullableTypes
    {
        public long? LongVal { get; set; }
        public int? IntVal { get; set; }
        public double? DoubleVal { get; set; }
        public string? TextVal { get; set; }
        public bool? BoolVal { get; set; }
    }

    public sealed class FloatTypes
    {
        public double DoubleVal { get; set; }
        public float FloatVal { get; set; }
        public decimal DecimalVal { get; set; }
    }

    public sealed class TextConversions
    {
        public string StringVal { get; set; } = string.Empty;
        public DateTime DateVal { get; set; }
        public DateTimeOffset DateOffsetVal { get; set; }
        public Guid GuidVal { get; set; }
    }

    public sealed class BlobType
    {
        public byte[] Data { get; set; } = [];
    }

    public enum Status
    {
        Active,
        Inactive,
    }

    public sealed class EnumFromText
    {
        public Status Status { get; set; }
    }

    public sealed class EnumFromInteger
    {
        public Status Status { get; set; }
    }

    public sealed class ReadOnlyPropType
    {
        public long Id { get; set; }
        public string Computed => $"Item-{Id}";
    }

    public sealed class TextNumericTypes
    {
        public decimal DecimalVal { get; set; }
        public double DoubleVal { get; set; }
        public float FloatVal { get; set; }
        public int IntVal { get; set; }
        public long LongVal { get; set; }
    }

    #endregion

    #region MapTo<T> - Basic Mapping

    [Fact]
    public void MapTo_BasicProperties_MapsCorrectly()
    {
        StatementResult result = CreateResult(
            ["id", "name", "age"],
            [
                [Value.Integer(1), Value.Text("Alice"), Value.Integer(30)],
                [Value.Integer(2), Value.Text("Bob"), Value.Integer(25)],
            ]
        );

        List<SimpleUser> users = result.MapTo<SimpleUser>();

        Assert.Equal(2, users.Count);
        Assert.Equal(1L, users[0].Id);
        Assert.Equal("Alice", users[0].Name);
        Assert.Equal(30, users[0].Age);
        Assert.Equal(2L, users[1].Id);
        Assert.Equal("Bob", users[1].Name);
        Assert.Equal(25, users[1].Age);
    }

    [Fact]
    public void MapTo_EmptyRows_ReturnsEmptyList()
    {
        StatementResult result = CreateResult(["id", "name"], []);

        List<SimpleUser> users = result.MapTo<SimpleUser>();

        Assert.Empty(users);
    }

    [Fact]
    public void MapTo_CaseInsensitiveColumnMatch()
    {
        StatementResult result = CreateResult(
            ["ID", "NAME", "AGE"],
            [
                [Value.Integer(1), Value.Text("Alice"), Value.Integer(30)],
            ]
        );

        List<SimpleUser> users = result.MapTo<SimpleUser>();

        Assert.Single(users);
        Assert.Equal(1L, users[0].Id);
        Assert.Equal("Alice", users[0].Name);
    }

    [Fact]
    public void MapTo_ColumnNameAttribute_MapsExplicitly()
    {
        StatementResult result = CreateResult(
            ["user_id", "full_name"],
            [
                [Value.Integer(42), Value.Text("Charlie")],
            ]
        );

        List<UserWithColumnName> users = result.MapTo<UserWithColumnName>();

        Assert.Single(users);
        Assert.Equal(42L, users[0].Id);
        Assert.Equal("Charlie", users[0].Name);
    }

    [Fact]
    public void MapTo_ExtraColumns_AreIgnored()
    {
        StatementResult result = CreateResult(
            ["id", "name", "extra_column"],
            [
                [Value.Integer(1), Value.Text("Alice"), Value.Text("ignored")],
            ]
        );

        List<SimpleUser> users = result.MapTo<SimpleUser>();

        Assert.Single(users);
        Assert.Equal(1L, users[0].Id);
        Assert.Equal("Alice", users[0].Name);
    }

    [Fact]
    public void MapTo_MissingColumns_LeavesDefaults()
    {
        StatementResult result = CreateResult(
            ["id"],
            [
                [Value.Integer(1)],
            ]
        );

        List<SimpleUser> users = result.MapTo<SimpleUser>();

        Assert.Single(users);
        Assert.Equal(1L, users[0].Id);
        Assert.Equal(string.Empty, users[0].Name);
        Assert.Equal(0, users[0].Age);
    }

    [Fact]
    public void MapTo_ReadOnlyProperties_AreSkipped()
    {
        StatementResult result = CreateResult(
            ["id"],
            [
                [Value.Integer(5)],
            ]
        );

        List<ReadOnlyPropType> items = result.MapTo<ReadOnlyPropType>();

        Assert.Single(items);
        Assert.Equal(5L, items[0].Id);
        Assert.Equal("Item-5", items[0].Computed);
    }

    #endregion

    #region MapToFirstOrDefault<T>

    [Fact]
    public void MapToFirstOrDefault_WithRows_ReturnsFirst()
    {
        StatementResult result = CreateResult(
            ["id", "name", "age"],
            [
                [Value.Integer(1), Value.Text("Alice"), Value.Integer(30)],
                [Value.Integer(2), Value.Text("Bob"), Value.Integer(25)],
            ]
        );

        SimpleUser? user = result.MapToFirstOrDefault<SimpleUser>();

        Assert.NotNull(user);
        Assert.Equal(1L, user.Id);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void MapToFirstOrDefault_EmptyRows_ReturnsNull()
    {
        StatementResult result = CreateResult(["id", "name"], []);

        SimpleUser? user = result.MapToFirstOrDefault<SimpleUser>();

        Assert.Null(user);
    }

    #endregion

    #region Integer Conversions

    [Fact]
    public void MapTo_IntegerValue_ConvertsToAllIntTypes()
    {
        StatementResult result = CreateResult(
            ["longval", "intval", "shortval", "byteval", "boolval", "uintval"],
            [
                [
                    Value.Integer(100),
                    Value.Integer(42),
                    Value.Integer(7),
                    Value.Integer(255),
                    Value.Integer(1),
                    Value.Integer(999),
                ],
            ]
        );

        List<AllIntegerTypes> items = result.MapTo<AllIntegerTypes>();

        Assert.Single(items);
        Assert.Equal(100L, items[0].LongVal);
        Assert.Equal(42, items[0].IntVal);
        Assert.Equal((short)7, items[0].ShortVal);
        Assert.Equal((byte)255, items[0].ByteVal);
        Assert.True(items[0].BoolVal);
        Assert.Equal(999u, items[0].UintVal);
    }

    [Fact]
    public void MapTo_IntegerValue_BoolFalse()
    {
        StatementResult result = CreateResult(
            ["boolval"],
            [
                [Value.Integer(0)],
            ]
        );

        List<AllIntegerTypes> items = result.MapTo<AllIntegerTypes>();

        Assert.False(items[0].BoolVal);
    }

    #endregion

    #region Text Conversions

    [Fact]
    public void MapTo_TextValue_ConvertsToString()
    {
        StatementResult result = CreateResult(
            ["stringval", "dateval", "dateoffsetval", "guidval"],
            [
                [
                    Value.Text("hello"),
                    Value.Text("2024-06-15T10:30:00Z"),
                    Value.Text("2024-06-15T10:30:00+02:00"),
                    Value.Text("550e8400-e29b-41d4-a716-446655440000"),
                ],
            ]
        );

        List<TextConversions> items = result.MapTo<TextConversions>();

        Assert.Single(items);
        Assert.Equal("hello", items[0].StringVal);
        Assert.Equal(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc), items[0].DateVal);
        Assert.Equal(
            new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.FromHours(2)),
            items[0].DateOffsetVal
        );
        Assert.Equal(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), items[0].GuidVal);
    }

    [Fact]
    public void MapTo_TextValue_ConvertsToEnum()
    {
        StatementResult result = CreateResult(
            ["status"],
            [
                [Value.Text("Active")],
            ]
        );

        List<EnumFromText> items = result.MapTo<EnumFromText>();

        Assert.Equal(Status.Active, items[0].Status);
    }

    [Fact]
    public void MapTo_TextValue_ConvertsToEnumCaseInsensitive()
    {
        StatementResult result = CreateResult(
            ["status"],
            [
                [Value.Text("inactive")],
            ]
        );

        List<EnumFromText> items = result.MapTo<EnumFromText>();

        Assert.Equal(Status.Inactive, items[0].Status);
    }

    #endregion

    #region Text-to-Numeric Conversions

    [Fact]
    public void MapTo_TextValue_ConvertsToDecimal()
    {
        StatementResult result = CreateResult(
            ["decimalval"],
            [
                [Value.Text("99.99")],
            ]
        );

        List<TextNumericTypes> items = result.MapTo<TextNumericTypes>();

        Assert.Single(items);
        Assert.Equal(99.99m, items[0].DecimalVal);
    }

    [Fact]
    public void MapTo_TextValue_ConvertsToDouble()
    {
        StatementResult result = CreateResult(
            ["doubleval"],
            [
                [Value.Text("3.14")],
            ]
        );

        List<TextNumericTypes> items = result.MapTo<TextNumericTypes>();

        Assert.Equal(3.14, items[0].DoubleVal);
    }

    [Fact]
    public void MapTo_TextValue_ConvertsToFloat()
    {
        StatementResult result = CreateResult(
            ["floatval"],
            [
                [Value.Text("2.5")],
            ]
        );

        List<TextNumericTypes> items = result.MapTo<TextNumericTypes>();

        Assert.Equal(2.5f, items[0].FloatVal);
    }

    [Fact]
    public void MapTo_TextValue_ConvertsToInt()
    {
        StatementResult result = CreateResult(
            ["intval"],
            [
                [Value.Text("42")],
            ]
        );

        List<TextNumericTypes> items = result.MapTo<TextNumericTypes>();

        Assert.Equal(42, items[0].IntVal);
    }

    [Fact]
    public void MapTo_TextValue_ConvertsToLong()
    {
        StatementResult result = CreateResult(
            ["longval"],
            [
                [Value.Text("9999999999")],
            ]
        );

        List<TextNumericTypes> items = result.MapTo<TextNumericTypes>();

        Assert.Equal(9999999999L, items[0].LongVal);
    }

    [Fact]
    public void MapTo_TextValue_DecimalWithHighPrecision()
    {
        StatementResult result = CreateResult(
            ["decimalval"],
            [
                [Value.Text("12345.67")],
            ]
        );

        List<TextNumericTypes> items = result.MapTo<TextNumericTypes>();

        Assert.Equal(12345.67m, items[0].DecimalVal);
    }

    #endregion

    #region Null Handling

    [Fact]
    public void MapTo_NullValue_SetsNullableToNull()
    {
        StatementResult result = CreateResult(
            ["longval", "intval", "doubleval", "textval", "boolval"],
            [
                [Value.Null(), Value.Null(), Value.Null(), Value.Null(), Value.Null()],
            ]
        );

        List<NullableTypes> items = result.MapTo<NullableTypes>();

        Assert.Single(items);
        Assert.Null(items[0].LongVal);
        Assert.Null(items[0].IntVal);
        Assert.Null(items[0].DoubleVal);
        Assert.Null(items[0].TextVal);
        Assert.Null(items[0].BoolVal);
    }

    [Fact]
    public void MapTo_NullValue_ThrowsForNonNullableValueType()
    {
        StatementResult result = CreateResult(
            ["id"],
            [
                [Value.Null()],
            ]
        );

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            result.MapTo<SimpleUser>()
        );

        Assert.Contains("Id", ex.Message);
        Assert.Contains("null", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MapTo_NullValue_SetsReferenceTypeToNull()
    {
        StatementResult result = CreateResult(
            ["name"],
            [
                [Value.Null()],
            ]
        );

        SimpleUser? user = result.MapToFirstOrDefault<SimpleUser>();

        Assert.NotNull(user);
        Assert.Null(user.Name);
    }

    #endregion
}
