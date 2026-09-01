using System.IO;
using System.Reflection;
using System.Text.Json;
using DevToys.MCP.Tools;
using OneOf;

namespace DevToys.UnitTests.Mcp;

public class JsonSchemaGeneratorTests
{
    private enum SampleMode
    {
        Alpha,
        Beta,
        Gamma
    }

    private sealed class SampleTool
    {
        public string Text { get; set; } = string.Empty;

        public bool Flag { get; set; } = true;

        public int Count { get; set; } = 5;

        public SampleMode Mode { get; set; } = SampleMode.Beta;

        public OneOf<string, FileInfo> Source { get; set; }

        public string[] Items { get; set; } = System.Array.Empty<string>();
    }

    private static CommandLineOptionDescriptor Option(string propertyName, string name, bool required = false, object? defaultValue = null)
    {
        PropertyInfo property = typeof(SampleTool).GetProperty(propertyName)!;
        var attribute = new CommandLineOptionAttribute { Name = name, IsRequired = required };
        return new CommandLineOptionDescriptor(property, attribute, name, description: null, defaultValue);
    }

    [Fact]
    public void CreateInputSchema_MapsTypes_AndRequired()
    {
        var options = new[]
        {
            Option(nameof(SampleTool.Text), "text", required: true),
            Option(nameof(SampleTool.Flag), "flag", defaultValue: true),
            Option(nameof(SampleTool.Count), "count", defaultValue: 5),
            Option(nameof(SampleTool.Mode), "mode", defaultValue: SampleMode.Beta),
            Option(nameof(SampleTool.Source), "source"),
            Option(nameof(SampleTool.Items), "items", required: true)
        };

        JsonElement schema = JsonSchemaGenerator.CreateInputSchema(options);

        schema.GetProperty("type").GetString().Should().Be("object");
        JsonElement properties = schema.GetProperty("properties");

        properties.GetProperty("text").GetProperty("type").GetString().Should().Be("string");

        properties.GetProperty("flag").GetProperty("type").GetString().Should().Be("boolean");
        properties.GetProperty("flag").GetProperty("default").GetBoolean().Should().BeTrue();

        properties.GetProperty("count").GetProperty("type").GetString().Should().Be("integer");
        properties.GetProperty("count").GetProperty("default").GetInt32().Should().Be(5);

        JsonElement mode = properties.GetProperty("mode");
        mode.GetProperty("type").GetString().Should().Be("string");
        mode.GetProperty("enum").EnumerateArray().Select(e => e.GetString()).Should().Equal("Alpha", "Beta", "Gamma");
        mode.GetProperty("default").GetString().Should().Be("Beta");

        // OneOf<string, FileInfo> is advertised as a plain string.
        properties.GetProperty("source").GetProperty("type").GetString().Should().Be("string");

        JsonElement items = properties.GetProperty("items");
        items.GetProperty("type").GetString().Should().Be("array");
        items.GetProperty("items").GetProperty("type").GetString().Should().Be("string");

        schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).Should().Equal("text", "items");
    }

    [Fact]
    public void CreateInputSchema_RequiredOption_HasNoDefault()
    {
        var options = new[] { Option(nameof(SampleTool.Text), "text", required: true, defaultValue: "x") };

        JsonElement schema = JsonSchemaGenerator.CreateInputSchema(options);

        JsonElement text = schema.GetProperty("properties").GetProperty("text");
        text.TryGetProperty("default", out _).Should().BeFalse();
    }

    [Fact]
    public void CreateInputSchema_NoRequiredOptions_OmitsRequiredArray()
    {
        var options = new[] { Option(nameof(SampleTool.Flag), "flag") };

        JsonElement schema = JsonSchemaGenerator.CreateInputSchema(options);

        schema.TryGetProperty("required", out _).Should().BeFalse();
    }
}
