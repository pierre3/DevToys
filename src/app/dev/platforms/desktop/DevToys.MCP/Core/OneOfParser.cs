using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DevToys.MCP.Core;

/// <summary>
/// Parses a string token into a <c>OneOf&lt;T1, T2, ...&gt;</c> value by attempting to convert it to each of the
/// union's generic type arguments and selecting the best match.
/// </summary>
/// <remarks>
/// Forked from DevToys.CLI's OneOfParser, itself forked from
/// https://github.com/dotnet/command-line-api/blob/d12a6ff555b33670defb2243daecdc247caf88e8/src/System.CommandLine/Binding/ArgumentConverter.StringConverters.cs#L15-L289
/// </remarks>
internal static class OneOfParser
{
    private delegate bool TryParseString(string token, [NotNullWhen(true)] out object? value);

    private static readonly Dictionary<Type, TryParseString> _stringParsers
        = new()
        {
            [typeof(bool)] = (string token, [NotNullWhen(true)] out object? value) =>
            {
                if (bool.TryParse(token, out bool parsed))
                {
                    value = parsed;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(DateOnly)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                if (DateOnly.TryParse(input, out DateOnly parsed))
                {
                    value = parsed;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(DateTime)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                if (DateTime.TryParse(input, out DateTime parsed))
                {
                    value = parsed;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(DateTimeOffset)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                if (DateTimeOffset.TryParse(input, out DateTimeOffset parsed))
                {
                    value = parsed;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(decimal)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                if (decimal.TryParse(input, out decimal parsed))
                {
                    value = parsed;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(DirectoryInfo)] = (string path, [NotNullWhen(true)] out object? value) =>
            {
                value = default;
                if (!string.IsNullOrEmpty(path))
                {
                    var directory = new DirectoryInfo(path);
                    if (directory.Exists)
                    {
                        value = directory;
                        return true;
                    }
                }
                return false;
            },

            [typeof(double)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                if (double.TryParse(input, out double parsed))
                {
                    value = parsed;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(FileInfo)] = (string path, [NotNullWhen(true)] out object? value) =>
            {
                value = default;
                if (!string.IsNullOrEmpty(path))
                {
                    var fileInfo = new FileInfo(path);
                    if (fileInfo.Exists)
                    {
                        value = fileInfo;
                        return true;
                    }
                }
                return false;
            },

            [typeof(FileSystemInfo)] = (string path, [NotNullWhen(true)] out object? value) =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    value = default;
                    return false;
                }
                if (Directory.Exists(path))
                {
                    value = new DirectoryInfo(path);
                }
                else if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                         path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                {
                    var directory = new DirectoryInfo(path);
                    if (!Directory.Exists(path))
                    {
                        value = default;
                        return false;
                    }
                    value = directory;
                }
                else
                {
                    var fileInfo = new FileInfo(path);
                    if (!fileInfo.Exists)
                    {
                        value = default;
                        return false;
                    }
                    value = fileInfo;
                }

                return true;
            },

            [typeof(float)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                if (float.TryParse(input, out float parsed))
                {
                    value = parsed;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(Guid)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                if (Guid.TryParse(input, out Guid parsed))
                {
                    value = parsed;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(int)] = (string token, [NotNullWhen(true)] out object? value) =>
            {
                if (int.TryParse(token, out int intValue))
                {
                    value = intValue;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(long)] = (string token, [NotNullWhen(true)] out object? value) =>
            {
                if (long.TryParse(token, out long longValue))
                {
                    value = longValue;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(short)] = (string token, [NotNullWhen(true)] out object? value) =>
            {
                if (short.TryParse(token, out short shortValue))
                {
                    value = shortValue;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(TimeOnly)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                if (TimeOnly.TryParse(input, out TimeOnly parsed))
                {
                    value = parsed;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(uint)] = (string token, [NotNullWhen(true)] out object? value) =>
            {
                if (uint.TryParse(token, out uint uintValue))
                {
                    value = uintValue;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(sbyte)] = (string token, [NotNullWhen(true)] out object? value) =>
            {
                if (sbyte.TryParse(token, out sbyte sbyteValue))
                {
                    value = sbyteValue;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(byte)] = (string token, [NotNullWhen(true)] out object? value) =>
            {
                if (byte.TryParse(token, out byte byteValue))
                {
                    value = byteValue;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(string)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                value = input;
                return true;
            },

            [typeof(ulong)] = (string token, [NotNullWhen(true)] out object? value) =>
            {
                if (ulong.TryParse(token, out ulong ulongValue))
                {
                    value = ulongValue;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(ushort)] = (string token, [NotNullWhen(true)] out object? value) =>
            {
                if (ushort.TryParse(token, out ushort ushortValue))
                {
                    value = ushortValue;
                    return true;
                }

                value = default;
                return false;
            },

            [typeof(TimeSpan)] = (string input, [NotNullWhen(true)] out object? value) =>
            {
                if (TimeSpan.TryParse(input, out TimeSpan timeSpan))
                {
                    value = timeSpan;
                    return true;
                }

                value = default;
                return false;
            },
        };

    /// <summary>
    /// Parses <paramref name="token"/> into a value of the given <c>OneOf&lt;...&gt;</c> <paramref name="oneOfType"/>.
    /// </summary>
    internal static object? ParseOneOf(Type oneOfType, string token)
    {
        // For each generic type argument, try to parse the token as that type.
        var parsedArguments = new Dictionary<int, object?>();
        for (int i = 0; i < oneOfType.GenericTypeArguments.Length; i++)
        {
            Type genericTypeArgument = oneOfType.GenericTypeArguments[i];

            try
            {
                object? parsedArgument = ParseScalarValue(token, genericTypeArgument);
                if (parsedArgument is not null)
                {
                    parsedArguments.Add(i, parsedArgument);
                }
            }
            catch
            {
            }
        }

        return SelectBestMatch(parsedArguments, oneOfType);
    }

    private static object? ParseScalarValue(string value, Type type)
    {
        // If the type is nullable, parse the value as the underlying type.
        if (type.TryGetNullableType(out Type? nullableType))
        {
            return ParseScalarValue(value, nullableType);
        }

        if (_stringParsers.TryGetValue(type, out TryParseString? tryConvert))
        {
            if (tryConvert(value, out object? converted))
            {
                return converted;
            }
            else
            {
                return null;
            }
        }

        if (type.IsEnum)
        {
            if (Enum.TryParse(type, value, ignoreCase: true, out object? converted))
            {
                return converted;
            }
        }

        return null;
    }

    private static object? SelectBestMatch(Dictionary<int, object?> parsedArguments, Type oneOfType)
    {
        if (parsedArguments.Count <= 0)
        {
            // The token could not be parsed as any of the generic type arguments.
            return null;
        }
        else if (parsedArguments.Count == 1)
        {
            KeyValuePair<int, object?> parsedArgument = parsedArguments.ToArray()[0];
            return CreateNewInstanceOfOneOf(oneOfType, parsedArgument.Key, parsedArgument.Value);
        }
        else
        {
            // The token could be parsed as more than one of the generic type arguments.
            // Return the first one in the priority order of _stringParsers keys. This way, for something like
            // OneOf<string, float>, "100" resolves to the float value rather than the string.
            foreach (Type parserType in _stringParsers.Keys)
            {
                foreach (KeyValuePair<int, object?> argument in parsedArguments)
                {
                    if (argument.Value is null)
                    {
                        continue;
                    }

                    if (parserType.IsAssignableFrom(argument.Value.GetType()))
                    {
                        return CreateNewInstanceOfOneOf(oneOfType, argument.Key, argument.Value);
                    }
                }
            }

            return null;
        }
    }

    private static object? CreateNewInstanceOfOneOf(Type oneOfType, int index, object? value)
    {
        // Assuming oneOfType is something like OneOf<T0, T1, T2>, call OneOf.FromT{index}(value).
        return oneOfType
            .GetMethod("FromT" + index, BindingFlags.Static | BindingFlags.Public)
            ?.Invoke(null, new object?[] { value });
    }
}
