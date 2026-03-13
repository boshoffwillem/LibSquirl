namespace LibSquirl.Protocol;

/// <summary>
///     Specifies the database column name that a property maps to
///     when using <see cref="StatementResultExtensions.MapTo{T}" />.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
