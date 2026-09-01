using System.Globalization;
using DevToys.MCP.Core;
using OneOf;

namespace DevToys.UnitTests.Mcp;

public class OneOfParserTests
{
    [Fact]
    public void ParseOneOf_StringOrFloat()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Type type = typeof(OneOf<string, float>);

        OneOfParser.ParseOneOf(type, "1.5").Should().Be((OneOf<string, float>)1.5f);
        OneOfParser.ParseOneOf(type, "5").Should().Be((OneOf<string, float>)5f);
        OneOfParser.ParseOneOf(type, "a").Should().Be((OneOf<string, float>)"a");
        OneOfParser.ParseOneOf(type, "").Should().Be((OneOf<string, float>)string.Empty);
    }

    [Fact]
    public void ParseOneOf_StringOrInt()
    {
        Type type = typeof(OneOf<string, int>);

        OneOfParser.ParseOneOf(type, "5").Should().Be((OneOf<string, int>)5);
        OneOfParser.ParseOneOf(type, "x").Should().Be((OneOf<string, int>)"x");
    }
}
