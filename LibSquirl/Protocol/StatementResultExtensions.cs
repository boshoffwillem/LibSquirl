using LibSquirl.Protocol.Models;

namespace LibSquirl.Protocol;

public static class StatementResultExtensions
{
    /// <summary>
    ///     Maps all rows in the result to a list of <typeparamref name="T" />.
    ///     Properties are matched to columns by <see cref="ColumnNameAttribute" />
    ///     or by case-insensitive property name.
    /// </summary>
    public static List<T> MapTo<T>(this StatementResult result)
        where T : new()
    {
        return StatementResultMapper.Map<T>(result);
    }

    /// <summary>
    ///     Maps the first row in the result to <typeparamref name="T" />,
    ///     or returns <c>null</c> if the result contains no rows.
    /// </summary>
    public static T? MapToFirstOrDefault<T>(this StatementResult result)
        where T : class, new()
    {
        List<T> items = StatementResultMapper.Map<T>(result);
        return items.Count > 0 ? items[0] : null;
    }
}
