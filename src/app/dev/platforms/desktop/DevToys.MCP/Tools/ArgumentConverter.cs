using System.Text.Json;
using DevToys.MCP.Core;

namespace DevToys.MCP.Tools;

/// <summary>
/// Converts an MCP argument (<see cref="JsonElement"/>) into the CLR value expected by an
/// <see cref="Api.CommandLineOptionAttribute"/>-decorated property, mirroring how DevToys.CLI binds
/// command-line tokens.
/// </summary>
internal static class ArgumentConverter
{
    /// <summary>
    /// Converts <paramref name="element"/> to a value assignable to <paramref name="targetType"/>.
    /// </summary>
    internal static object? Convert(JsonElement element, Type targetType)
    {
        Type nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (nonNullableType.IsEnumerable())
        {
            Type elementType = nonNullableType.GetElementTypeIfEnumerable() ?? typeof(string);
            return ConvertArray(element, nonNullableType, Nullable.GetUnderlyingType(elementType) ?? elementType);
        }

        return ConvertScalar(element, nonNullableType);
    }

    private static object? ConvertScalar(JsonElement element, Type type)
    {
        // OneOf<T1, T2, ...> options are supplied as a single string, parsed to the best matching type.
        if (type.IsAssignableTo(typeof(OneOf.IOneOf)))
        {
            return OneOfParser.ParseOneOf(type, GetString(element));
        }

        if (type == typeof(string))
        {
            return GetString(element);
        }

        if (type == typeof(bool))
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => bool.Parse(GetString(element))
            };
        }

        if (type.IsEnum)
        {
            return Enum.Parse(type, GetString(element), ignoreCase: true);
        }

        if (type == typeof(byte))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetByte() : byte.Parse(GetString(element));
        }

        if (type == typeof(sbyte))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetSByte() : sbyte.Parse(GetString(element));
        }

        if (type == typeof(short))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetInt16() : short.Parse(GetString(element));
        }

        if (type == typeof(ushort))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetUInt16() : ushort.Parse(GetString(element));
        }

        if (type == typeof(int))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetInt32() : int.Parse(GetString(element));
        }

        if (type == typeof(uint))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetUInt32() : uint.Parse(GetString(element));
        }

        if (type == typeof(long))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetInt64() : long.Parse(GetString(element));
        }

        if (type == typeof(ulong))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetUInt64() : ulong.Parse(GetString(element));
        }

        if (type == typeof(float))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetSingle() : float.Parse(GetString(element));
        }

        if (type == typeof(double))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : double.Parse(GetString(element));
        }

        if (type == typeof(decimal))
        {
            return element.ValueKind == JsonValueKind.Number ? element.GetDecimal() : decimal.Parse(GetString(element));
        }

        if (type == typeof(FileInfo))
        {
            return new FileInfo(GetString(element));
        }

        if (type == typeof(DirectoryInfo))
        {
            return new DirectoryInfo(GetString(element));
        }

        if (type == typeof(Guid))
        {
            return Guid.Parse(GetString(element));
        }

        if (type == typeof(char))
        {
            string text = GetString(element);
            return text.Length > 0 ? text[0] : default(char);
        }

        // Fall back to System.Text.Json for any other type (dates, TimeSpan, ...).
        return element.Deserialize(type);
    }

    private static object ConvertArray(JsonElement element, Type collectionType, Type elementType)
    {
        var items = new List<object?>();
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                items.Add(Convert(item, elementType));
            }
        }
        else
        {
            // Accept a single value where an array is expected.
            items.Add(Convert(element, elementType));
        }

        var array = Array.CreateInstance(elementType, items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            array.SetValue(items[i], i);
        }

        // T[] and interfaces such as IEnumerable<T>/IReadOnlyList<T> accept the array directly.
        if (collectionType.IsAssignableFrom(array.GetType()))
        {
            return array;
        }

        // Otherwise build a List<T> (covers List<T> and similar concrete collections).
        Type listType = typeof(List<>).MakeGenericType(elementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
        for (int i = 0; i < items.Count; i++)
        {
            list.Add(items[i]);
        }

        return list;
    }

    private static string GetString(JsonElement element)
        => element.ValueKind == JsonValueKind.String ? element.GetString()! : element.GetRawText();
}
