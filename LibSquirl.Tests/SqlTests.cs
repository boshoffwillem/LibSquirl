using LibSquirl.Protocol.Models;

namespace LibSquirl.Tests;

public class SqlTests
{
    // ── Arg (string) ────────────────────────────────────────────────────

    [Fact]
    public void Arg_String_CreatesTextValue()
    {
        NamedArg arg = Sql.Arg(":name", "alice");

        Assert.Equal(":name", arg.Name);
        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.Equal("alice", text.Val);
    }

    // ── Arg (Guid) ──────────────────────────────────────────────────────

    [Fact]
    public void Arg_Guid_CreatesTextValueWithStringRepresentation()
    {
        Guid id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        NamedArg arg = Sql.Arg(":id", id);

        Assert.Equal(":id", arg.Name);
        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.Equal("550e8400-e29b-41d4-a716-446655440000", text.Val);
    }

    // ── Arg (int) ───────────────────────────────────────────────────────

    [Fact]
    public void Arg_Int_CreatesIntegerValue()
    {
        NamedArg arg = Sql.Arg(":age", 42);

        Assert.Equal(":age", arg.Name);
        IntegerValue integer = Assert.IsType<IntegerValue>(arg.Value);
        Assert.Equal(42L, integer.Val);
    }

    // ── Arg (long) ──────────────────────────────────────────────────────

    [Fact]
    public void Arg_Long_CreatesIntegerValue()
    {
        NamedArg arg = Sql.Arg(":count", 9_999_999_999L);

        Assert.Equal(":count", arg.Name);
        IntegerValue integer = Assert.IsType<IntegerValue>(arg.Value);
        Assert.Equal(9_999_999_999L, integer.Val);
    }

    // ── Arg (bool) ──────────────────────────────────────────────────────

    [Fact]
    public void Arg_BoolTrue_CreatesIntegerOne()
    {
        NamedArg arg = Sql.Arg(":active", true);

        IntegerValue integer = Assert.IsType<IntegerValue>(arg.Value);
        Assert.Equal(1L, integer.Val);
    }

    [Fact]
    public void Arg_BoolFalse_CreatesIntegerZero()
    {
        NamedArg arg = Sql.Arg(":active", false);

        IntegerValue integer = Assert.IsType<IntegerValue>(arg.Value);
        Assert.Equal(0L, integer.Val);
    }

    // ── Arg (double) ────────────────────────────────────────────────────

    [Fact]
    public void Arg_Double_CreatesFloatValue()
    {
        NamedArg arg = Sql.Arg(":score", 3.14);

        Assert.Equal(":score", arg.Name);
        FloatValue f = Assert.IsType<FloatValue>(arg.Value);
        Assert.Equal(3.14, f.Val);
    }

    // ── Arg (decimal) ───────────────────────────────────────────────────

    [Fact]
    public void Arg_Decimal_CreatesF2FormattedTextValue()
    {
        NamedArg arg = Sql.Arg(":amount", 99.9m);

        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.Equal("99.90", text.Val);
    }

    [Fact]
    public void Arg_Decimal_RoundsToTwoDecimalPlaces()
    {
        NamedArg arg = Sql.Arg(":amount", 10.999m);

        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.Equal("11.00", text.Val);
    }

    // ── Arg (DateTime) ──────────────────────────────────────────────────

    [Fact]
    public void Arg_DateTime_CreatesUtcIso8601FormattedTextValue()
    {
        DateTime dt = new(2024, 6, 15, 10, 30, 45, 123, DateTimeKind.Utc);

        NamedArg arg = Sql.Arg(":created", dt);

        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.Equal("2024-06-15T10:30:45.123Z", text.Val);
    }

    [Fact]
    public void Arg_DateTime_LocalTime_IsConvertedToUtc()
    {
        DateTime local = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);

        NamedArg arg = Sql.Arg(":dt", local);

        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.EndsWith("Z", text.Val);
    }

    // ── ArgNullable (string?) ───────────────────────────────────────────

    [Fact]
    public void ArgNullable_StringWithValue_CreatesTextValue()
    {
        NamedArg arg = Sql.ArgNullable(":name", (string?)"hello");

        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.Equal("hello", text.Val);
    }

    [Fact]
    public void ArgNullable_StringNull_CreatesNullValue()
    {
        NamedArg arg = Sql.ArgNullable(":name", (string?)null);

        Assert.IsType<NullValue>(arg.Value);
    }

    // ── ArgNullable (Guid?) ─────────────────────────────────────────────

    [Fact]
    public void ArgNullable_GuidWithValue_CreatesTextValue()
    {
        Guid id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        NamedArg arg = Sql.ArgNullable(":id", (Guid?)id);

        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.Equal("550e8400-e29b-41d4-a716-446655440000", text.Val);
    }

    [Fact]
    public void ArgNullable_GuidNull_CreatesNullValue()
    {
        NamedArg arg = Sql.ArgNullable(":id", (Guid?)null);

        Assert.IsType<NullValue>(arg.Value);
    }

    // ── ArgNullable (int?) ──────────────────────────────────────────────

    [Fact]
    public void ArgNullable_IntWithValue_CreatesIntegerValue()
    {
        NamedArg arg = Sql.ArgNullable(":count", (int?)42);

        IntegerValue integer = Assert.IsType<IntegerValue>(arg.Value);
        Assert.Equal(42L, integer.Val);
    }

    [Fact]
    public void ArgNullable_IntNull_CreatesNullValue()
    {
        NamedArg arg = Sql.ArgNullable(":count", (int?)null);

        Assert.IsType<NullValue>(arg.Value);
    }

    // ── ArgNullable (long?) ─────────────────────────────────────────────

    [Fact]
    public void ArgNullable_LongWithValue_CreatesIntegerValue()
    {
        NamedArg arg = Sql.ArgNullable(":big", (long?)123456789L);

        IntegerValue integer = Assert.IsType<IntegerValue>(arg.Value);
        Assert.Equal(123456789L, integer.Val);
    }

    [Fact]
    public void ArgNullable_LongNull_CreatesNullValue()
    {
        NamedArg arg = Sql.ArgNullable(":big", (long?)null);

        Assert.IsType<NullValue>(arg.Value);
    }

    // ── ArgNullable (double?) ───────────────────────────────────────────

    [Fact]
    public void ArgNullable_DoubleWithValue_CreatesFloatValue()
    {
        NamedArg arg = Sql.ArgNullable(":lat", (double?)51.5074);

        FloatValue f = Assert.IsType<FloatValue>(arg.Value);
        Assert.Equal(51.5074, f.Val);
    }

    [Fact]
    public void ArgNullable_DoubleNull_CreatesNullValue()
    {
        NamedArg arg = Sql.ArgNullable(":lat", (double?)null);

        Assert.IsType<NullValue>(arg.Value);
    }

    // ── ArgNullable (decimal?) ──────────────────────────────────────────

    [Fact]
    public void ArgNullable_DecimalWithValue_CreatesF2FormattedTextValue()
    {
        NamedArg arg = Sql.ArgNullable(":price", (decimal?)49.5m);

        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.Equal("49.50", text.Val);
    }

    [Fact]
    public void ArgNullable_DecimalNull_CreatesNullValue()
    {
        NamedArg arg = Sql.ArgNullable(":price", (decimal?)null);

        Assert.IsType<NullValue>(arg.Value);
    }

    // ── ArgNullable (DateTime?) ─────────────────────────────────────────

    [Fact]
    public void ArgNullable_DateTimeWithValue_CreatesFormattedTextValue()
    {
        DateTime dt = new(2024, 12, 25, 0, 0, 0, DateTimeKind.Utc);

        NamedArg arg = Sql.ArgNullable(":date", (DateTime?)dt);

        TextValue text = Assert.IsType<TextValue>(arg.Value);
        Assert.Equal("2024-12-25T00:00:00.000Z", text.Val);
    }

    [Fact]
    public void ArgNullable_DateTimeNull_CreatesNullValue()
    {
        NamedArg arg = Sql.ArgNullable(":date", (DateTime?)null);

        Assert.IsType<NullValue>(arg.Value);
    }

    // ── In (Guid list) ──────────────────────────────────────────────────

    [Fact]
    public void In_Guids_CreatesNumberedPlaceholdersAndArgs()
    {
        Guid g1 = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid g2 = Guid.Parse("11111111-2222-3333-4444-555555555555");
        List<Guid> ids = [g1, g2];

        (string placeholders, NamedArg[] args) = Sql.In(":id", ids);

        Assert.Equal(":id0, :id1", placeholders);
        Assert.Equal(2, args.Length);
        Assert.Equal(":id0", args[0].Name);
        Assert.Equal(g1.ToString(), ((TextValue)args[0].Value).Val);
        Assert.Equal(":id1", args[1].Name);
        Assert.Equal(g2.ToString(), ((TextValue)args[1].Value).Val);
    }

    [Fact]
    public void In_Guids_SingleItem_CreatesSinglePlaceholder()
    {
        Guid g1 = Guid.NewGuid();
        List<Guid> ids = [g1];

        (string placeholders, NamedArg[] args) = Sql.In(":x", ids);

        Assert.Equal(":x0", placeholders);
        Assert.Single(args);
    }

    [Fact]
    public void In_Guids_EmptyList_ReturnsFalseCondition()
    {
        List<Guid> empty = [];

        (string placeholders, NamedArg[] args) = Sql.In(":id", empty);

        Assert.Equal("1=0", placeholders);
        Assert.Empty(args);
    }

    // ── In (string list) ────────────────────────────────────────────────

    [Fact]
    public void In_Strings_CreatesNumberedPlaceholdersAndArgs()
    {
        List<string> names = ["Monday", "Wednesday", "Friday"];

        (string placeholders, NamedArg[] args) = Sql.In(":day", names);

        Assert.Equal(":day0, :day1, :day2", placeholders);
        Assert.Equal(3, args.Length);
        Assert.Equal("Monday", ((TextValue)args[0].Value).Val);
        Assert.Equal("Wednesday", ((TextValue)args[1].Value).Val);
        Assert.Equal("Friday", ((TextValue)args[2].Value).Val);
    }

    [Fact]
    public void In_Strings_EmptyList_ReturnsFalseCondition()
    {
        List<string> empty = [];

        (string placeholders, NamedArg[] args) = Sql.In(":s", empty);

        Assert.Equal("1=0", placeholders);
        Assert.Empty(args);
    }

    // ── CombineArgs ─────────────────────────────────────────────────────

    [Fact]
    public void CombineArgs_MergesBaseAndExtraArgs()
    {
        NamedArg[] baseArgs = [Sql.Arg(":a", "one"), Sql.Arg(":b", "two")];
        NamedArg extra1 = Sql.Arg(":c", "three");
        NamedArg extra2 = Sql.Arg(":d", "four");

        NamedArg[] combined = Sql.CombineArgs(baseArgs, extra1, extra2);

        Assert.Equal(4, combined.Length);
        Assert.Equal(":a", combined[0].Name);
        Assert.Equal(":b", combined[1].Name);
        Assert.Equal(":c", combined[2].Name);
        Assert.Equal(":d", combined[3].Name);
    }

    [Fact]
    public void CombineArgs_NoExtra_ReturnsCopyOfOriginal()
    {
        NamedArg[] baseArgs = [Sql.Arg(":a", "one")];

        NamedArg[] combined = Sql.CombineArgs(baseArgs);

        Assert.Single(combined);
        Assert.Equal(":a", combined[0].Name);
    }

    [Fact]
    public void CombineArgs_EmptyBase_ReturnsExtraOnly()
    {
        NamedArg[] baseArgs = [];
        NamedArg extra = Sql.Arg(":x", 1);

        NamedArg[] combined = Sql.CombineArgs(baseArgs, extra);

        Assert.Single(combined);
        Assert.Equal(":x", combined[0].Name);
    }

    // ── In + CombineArgs integration ────────────────────────────────────

    [Fact]
    public void In_And_CombineArgs_WorkTogether()
    {
        List<Guid> ids = [Guid.NewGuid(), Guid.NewGuid()];
        (string inSql, NamedArg[] inArgs) = Sql.In(":id", ids);

        NamedArg[] allArgs = Sql.CombineArgs(inArgs, Sql.Arg(":status", "Active"));

        Assert.Equal(3, allArgs.Length);
        Assert.Equal(":id0", allArgs[0].Name);
        Assert.Equal(":id1", allArgs[1].Name);
        Assert.Equal(":status", allArgs[2].Name);
        Assert.Contains(":id0", inSql);
        Assert.Contains(":id1", inSql);
    }
}
