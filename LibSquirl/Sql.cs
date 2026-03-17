using System.Globalization;
using LibSquirl.Protocol.Models;

namespace LibSquirl;

/// <summary>
///     Convenience factory methods for building <see cref="NamedArg" /> parameters
///     and SQL IN-clause placeholders.
/// </summary>
public static class Sql
{
    // ── Single-value Arg factories ──────────────────────────────────────

    public static NamedArg Arg(string name, string value) =>
        new() { Name = name, Value = Value.Text(value) };

    public static NamedArg Arg(string name, Guid value) =>
        new() { Name = name, Value = Value.Text(value.ToString()) };

    public static NamedArg Arg(string name, int value) =>
        new() { Name = name, Value = Value.Integer(value) };

    public static NamedArg Arg(string name, long value) =>
        new() { Name = name, Value = Value.Integer(value) };

    public static NamedArg Arg(string name, bool value) =>
        new() { Name = name, Value = Value.Integer(value ? 1 : 0) };

    public static NamedArg Arg(string name, double value) =>
        new() { Name = name, Value = Value.Float(value) };

    public static NamedArg Arg(string name, decimal value) =>
        new()
        {
            Name = name,
            Value = Value.Text(value.ToString("F2", CultureInfo.InvariantCulture)),
        };

    public static NamedArg Arg(string name, DateTime value) =>
        new()
        {
            Name = name,
            Value = Value.Text(
                value
                    .ToUniversalTime()
                    .ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
            ),
        };

    // ── Nullable Arg factories ──────────────────────────────────────────

    public static NamedArg ArgNullable(string name, string? value) =>
        new() { Name = name, Value = value is not null ? Value.Text(value) : Value.Null() };

    public static NamedArg ArgNullable(string name, Guid? value) =>
        new()
        {
            Name = name,
            Value = value.HasValue ? Value.Text(value.Value.ToString()) : Value.Null(),
        };

    public static NamedArg ArgNullable(string name, int? value) =>
        new() { Name = name, Value = value.HasValue ? Value.Integer(value.Value) : Value.Null() };

    public static NamedArg ArgNullable(string name, long? value) =>
        new() { Name = name, Value = value.HasValue ? Value.Integer(value.Value) : Value.Null() };

    public static NamedArg ArgNullable(string name, double? value) =>
        new() { Name = name, Value = value.HasValue ? Value.Float(value.Value) : Value.Null() };

    public static NamedArg ArgNullable(string name, decimal? value) =>
        new()
        {
            Name = name,
            Value = value.HasValue
                ? Value.Text(value.Value.ToString("F2", CultureInfo.InvariantCulture))
                : Value.Null(),
        };

    public static NamedArg ArgNullable(string name, DateTime? value) =>
        value.HasValue
            ? Arg(name, value.Value)
            : new NamedArg { Name = name, Value = Value.Null() };

    // ── IN-clause builders ──────────────────────────────────────────────

    /// <summary>
    ///     Builds a SQL IN-clause placeholder string and matching <see cref="NamedArg" /> array
    ///     for a list of <see cref="Guid" /> values.
    /// </summary>
    /// <param name="prefix">
    ///     Parameter name prefix including the colon, e.g. <c>":id"</c>.
    ///     Generates <c>:id0, :id1, :id2, ...</c>
    /// </param>
    /// <param name="values">The values to parameterize.</param>
    /// <returns>
    ///     A tuple of (placeholders SQL fragment, named args array).
    ///     Returns <c>("1=0", [])</c> for an empty list so the query is always syntactically valid.
    /// </returns>
    public static (string Placeholders, NamedArg[] Args) In(
        string prefix,
        IReadOnlyList<Guid> values
    )
    {
        if (values.Count == 0)
        {
            return ("1=0", []);
        }

        string[] placeholders = new string[values.Count];
        NamedArg[] args = new NamedArg[values.Count];

        for (int i = 0; i < values.Count; i++)
        {
            placeholders[i] = $"{prefix}{i}";
            args[i] = new NamedArg
            {
                Name = $"{prefix}{i}",
                Value = Value.Text(values[i].ToString()),
            };
        }

        return (string.Join(", ", placeholders), args);
    }

    /// <summary>
    ///     Builds a SQL IN-clause placeholder string and matching <see cref="NamedArg" /> array
    ///     for a list of <see cref="string" /> values.
    /// </summary>
    public static (string Placeholders, NamedArg[] Args) In(
        string prefix,
        IReadOnlyList<string> values
    )
    {
        if (values.Count == 0)
        {
            return ("1=0", []);
        }

        string[] placeholders = new string[values.Count];
        NamedArg[] args = new NamedArg[values.Count];

        for (int i = 0; i < values.Count; i++)
        {
            placeholders[i] = $"{prefix}{i}";
            args[i] = new NamedArg { Name = $"{prefix}{i}", Value = Value.Text(values[i]) };
        }

        return (string.Join(", ", placeholders), args);
    }

    // ── Arg list combiner ───────────────────────────────────────────────

    /// <summary>
    ///     Combines multiple <see cref="NamedArg" /> sources into a single array.
    ///     Useful when a query has both fixed args and IN-clause args.
    /// </summary>
    /// <example>
    ///     <code>
    ///     var (inSql, inArgs) = Sql.In(":id", ids);
    ///     var allArgs = Sql.CombineArgs(inArgs, Sql.Arg(":status", "Active"));
    ///     </code>
    /// </example>
    public static NamedArg[] CombineArgs(NamedArg[] args, params NamedArg[] extra)
    {
        NamedArg[] combined = new NamedArg[args.Length + extra.Length];
        args.CopyTo(combined, 0);
        extra.CopyTo(combined, args.Length);
        return combined;
    }
}
