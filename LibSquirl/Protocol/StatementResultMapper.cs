using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using LibSquirl.Protocol.Models;

namespace LibSquirl.Protocol;

internal static class StatementResultMapper
{
    private static readonly ConcurrentDictionary<Type, PropertyMapping[]> s_cache = new();

    internal static List<T> Map<T>(StatementResult result)
        where T : new()
    {
        PropertyMapping[] mappings = s_cache.GetOrAdd(typeof(T), BuildPropertyMappings);
        Dictionary<string, int> columnIndex = BuildColumnIndex(result.Cols);
        int[] propertyToColumn = ResolveColumns(mappings, columnIndex);

        List<T> items = new(result.Rows.Count);

        for (int rowIdx = 0; rowIdx < result.Rows.Count; rowIdx++)
        {
            List<Value> row = result.Rows[rowIdx];
            T item = new();

            for (int i = 0; i < mappings.Length; i++)
            {
                int colIdx = propertyToColumn[i];
                if (colIdx < 0)
                {
                    continue;
                }

                Value value = row[colIdx];
                object? converted = ConvertValue(
                    value,
                    mappings[i].PropertyType,
                    mappings[i].PropertyName
                );
                mappings[i].Setter(item, converted);
            }

            items.Add(item);
        }

        return items;
    }

    private static PropertyMapping[] BuildPropertyMappings(Type type)
    {
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        List<PropertyMapping> mappings = new(properties.Length);

        for (int i = 0; i < properties.Length; i++)
        {
            PropertyInfo prop = properties[i];
            if (!prop.CanWrite)
            {
                continue;
            }

            ColumnNameAttribute? attr = prop.GetCustomAttribute<ColumnNameAttribute>();
            string columnName = attr?.Name ?? prop.Name;

            mappings.Add(
                new PropertyMapping
                {
                    ColumnName = columnName,
                    PropertyName = prop.Name,
                    PropertyType = prop.PropertyType,
                    Setter = prop.SetValue,
                }
            );
        }

        return mappings.ToArray();
    }

    private static Dictionary<string, int> BuildColumnIndex(List<Column> cols)
    {
        Dictionary<string, int> index = new(cols.Count, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < cols.Count; i++)
        {
            string? name = cols[i].Name;
            if (name is not null)
            {
                index[name] = i;
            }
        }

        return index;
    }

    private static int[] ResolveColumns(
        PropertyMapping[] mappings,
        Dictionary<string, int> columnIndex
    )
    {
        int[] result = new int[mappings.Length];

        for (int i = 0; i < mappings.Length; i++)
        {
            if (columnIndex.TryGetValue(mappings[i].ColumnName, out int colIdx))
            {
                result[i] = colIdx;
            }
            else
            {
                result[i] = -1;
            }
        }

        return result;
    }

    private static object? ConvertValue(Value value, Type targetType, string propertyName)
    {
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        bool isNullable =
            !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;

        if (value is NullValue)
        {
            if (!isNullable)
            {
                throw new InvalidOperationException(
                    $"Cannot assign null to non-nullable property '{propertyName}' of type '{targetType.Name}'."
                );
            }

            return null;
        }

        return value switch
        {
            IntegerValue iv => ConvertInteger(iv.Val, underlyingType, propertyName),
            FloatValue fv => ConvertFloat(fv.Val, underlyingType, propertyName),
            TextValue tv => ConvertText(tv.Val, underlyingType, propertyName),
            BlobValue bv => ConvertBlob(bv.Val, underlyingType, propertyName),
            _ => throw new InvalidOperationException(
                $"Unsupported Value type '{value.GetType().Name}' for property '{propertyName}'."
            ),
        };
    }

    private static object ConvertInteger(long val, Type targetType, string propertyName)
    {
        if (targetType == typeof(long))
        {
            return val;
        }

        if (targetType == typeof(int))
        {
            return checked((int)val);
        }

        if (targetType == typeof(short))
        {
            return checked((short)val);
        }

        if (targetType == typeof(byte))
        {
            return checked((byte)val);
        }

        if (targetType == typeof(bool))
        {
            return val != 0;
        }

        if (targetType == typeof(ulong))
        {
            return checked((ulong)val);
        }

        if (targetType == typeof(uint))
        {
            return checked((uint)val);
        }

        if (targetType == typeof(ushort))
        {
            return checked((ushort)val);
        }

        if (targetType == typeof(double))
        {
            return (double)val;
        }

        if (targetType == typeof(float))
        {
            return (float)val;
        }

        if (targetType == typeof(decimal))
        {
            return (decimal)val;
        }

        if (targetType.IsEnum)
        {
            return Enum.ToObject(targetType, val);
        }

        throw new InvalidOperationException(
            $"Cannot convert IntegerValue to '{targetType.Name}' for property '{propertyName}'."
        );
    }

    private static object ConvertFloat(double val, Type targetType, string propertyName)
    {
        if (targetType == typeof(double))
        {
            return val;
        }

        if (targetType == typeof(float))
        {
            return (float)val;
        }

        if (targetType == typeof(decimal))
        {
            return (decimal)val;
        }

        throw new InvalidOperationException(
            $"Cannot convert FloatValue to '{targetType.Name}' for property '{propertyName}'."
        );
    }

    private static object ConvertText(string val, Type targetType, string propertyName)
    {
        if (targetType == typeof(string))
        {
            return val;
        }

        if (targetType == typeof(DateTime))
        {
            return DateTime.Parse(val, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        if (targetType == typeof(DateTimeOffset))
        {
            return DateTimeOffset.Parse(
                val,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            );
        }

        if (targetType == typeof(Guid))
        {
            return Guid.Parse(val);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, val, true);
        }

        // Fallback: try JSON deserialization for complex types (e.g., IReadOnlyList<string>)
        try
        {
            return JsonSerializer.Deserialize(val, targetType)
                ?? throw new InvalidOperationException(
                    $"JSON deserialization returned null for property '{propertyName}'."
                );
        }
        catch (JsonException)
        {
            throw new InvalidOperationException(
                $"Cannot convert TextValue to '{targetType.Name}' for property '{propertyName}'."
            );
        }
    }

    private static object ConvertBlob(byte[] val, Type targetType, string propertyName)
    {
        if (targetType == typeof(byte[]))
        {
            return val;
        }

        throw new InvalidOperationException(
            $"Cannot convert BlobValue to '{targetType.Name}' for property '{propertyName}'."
        );
    }

    private sealed class PropertyMapping
    {
        public required string ColumnName { get; init; }
        public required string PropertyName { get; init; }
        public required Type PropertyType { get; init; }
        public required Action<object?, object?> Setter { get; init; }
    }
}
