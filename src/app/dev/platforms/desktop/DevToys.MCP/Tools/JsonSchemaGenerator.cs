using System.Text.Json;
using System.Text.Json.Nodes;
using DevToys.MCP.Core;

namespace DevToys.MCP.Tools;

/// <summary>
/// Generates a JSON schema describing the input arguments of an <see cref="Api.ICommandLineTool"/>,
/// derived from its <see cref="Api.CommandLineOptionAttribute"/>-decorated properties.
/// </summary>
internal static class JsonSchemaGenerator
{
    /// <summary>
    /// Builds a JSON schema object of the form
    /// <c>{ "type": "object", "properties": { ... }, "required": [ ... ] }</c> for the given options.
    /// </summary>
    internal static JsonElement CreateInputSchema(IReadOnlyList<CommandLineOptionDescriptor> options)
    {
        Guard.IsNotNull(options);

        var properties = new JsonObject();
        var required = new JsonArray();

        for (int i = 0; i < options.Count; i++)
        {
            CommandLineOptionDescriptor option = options[i];
            properties[option.Name] = CreatePropertySchema(option);

            if (option.IsRequired)
            {
                required.Add(option.Name);
            }
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return JsonSerializer.SerializeToElement(schema);
    }

    private static JsonObject CreatePropertySchema(CommandLineOptionDescriptor option)
    {
        JsonObject node = MapType(option.Property.PropertyType);

        if (!string.IsNullOrWhiteSpace(option.Description))
        {
            node["description"] = option.Description;
        }

        // Only advertise a default for optional options; a required option with a default is contradictory.
        if (!option.IsRequired)
        {
            JsonNode? defaultValue = TryConvertDefaultValue(option.DefaultValue);
            if (defaultValue is not null)
            {
                node["default"] = defaultValue;
            }
        }

        return node;
    }

    private static JsonObject MapType(Type type)
    {
        Type nonNullableType = Nullable.GetUnderlyingType(type) ?? type;

        // An enumerable (but not a string) maps to a JSON array of the element type.
        if (nonNullableType.IsEnumerable())
        {
            Type? elementType = nonNullableType.GetElementTypeIfEnumerable();
            JsonObject itemsSchema
                = elementType is not null
                ? MapScalar(Nullable.GetUnderlyingType(elementType) ?? elementType)
                : new JsonObject { ["type"] = "string" };

            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = itemsSchema
            };
        }

        return MapScalar(nonNullableType);
    }

    private static JsonObject MapScalar(Type type)
    {
        Type nonNullableType = Nullable.GetUnderlyingType(type) ?? type;

        // OneOf<T1, T2, ...> options are provided as a single string and parsed to the best matching type
        // at invocation time, so they are advertised as a string.
        if (nonNullableType.IsAssignableTo(typeof(OneOf.IOneOf)))
        {
            return new JsonObject { ["type"] = "string" };
        }

        if (nonNullableType == typeof(bool))
        {
            return new JsonObject { ["type"] = "boolean" };
        }

        if (nonNullableType.IsEnum)
        {
            var enumValues = new JsonArray();
            foreach (string name in Enum.GetNames(nonNullableType))
            {
                enumValues.Add(name);
            }

            return new JsonObject
            {
                ["type"] = "string",
                ["enum"] = enumValues
            };
        }

        if (IsIntegerType(nonNullableType))
        {
            return new JsonObject { ["type"] = "integer" };
        }

        if (nonNullableType == typeof(float) || nonNullableType == typeof(double) || nonNullableType == typeof(decimal))
        {
            return new JsonObject { ["type"] = "number" };
        }

        // Everything else (string, char, Guid, dates, TimeSpan, FileInfo, DirectoryInfo, ...) is a string.
        return new JsonObject { ["type"] = "string" };
    }

    private static bool IsIntegerType(Type type)
        => type == typeof(byte)
        || type == typeof(sbyte)
        || type == typeof(short)
        || type == typeof(ushort)
        || type == typeof(int)
        || type == typeof(uint)
        || type == typeof(long)
        || type == typeof(ulong);

    private static JsonNode? TryConvertDefaultValue(object? defaultValue)
    {
        return defaultValue switch
        {
            null => null,
            bool value => JsonValue.Create(value),
            string value => JsonValue.Create(value),
            byte value => JsonValue.Create(value),
            sbyte value => JsonValue.Create(value),
            short value => JsonValue.Create(value),
            ushort value => JsonValue.Create(value),
            int value => JsonValue.Create(value),
            uint value => JsonValue.Create(value),
            long value => JsonValue.Create(value),
            ulong value => JsonValue.Create(value),
            float value => JsonValue.Create(value),
            double value => JsonValue.Create(value),
            decimal value => JsonValue.Create(value),
            Enum value => JsonValue.Create(value.ToString()),
            _ => null
        };
    }
}
