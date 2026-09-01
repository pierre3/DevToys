using System.IO;
using System.Text.Json;
using DevToys.MCP.Tools;
using OneOf;

namespace DevToys.UnitTests.Mcp;

public class ArgumentConverterTests
{
    private enum Color
    {
        Red,
        Green,
        Blue
    }

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement;

    [Fact]
    public void Convert_String()
        => ArgumentConverter.Convert(Json("\"hello\""), typeof(string)).Should().Be("hello");

    [Fact]
    public void Convert_Bool_FromJsonBool()
        => ArgumentConverter.Convert(Json("true"), typeof(bool)).Should().Be(true);

    [Fact]
    public void Convert_Bool_FromString()
        => ArgumentConverter.Convert(Json("\"true\""), typeof(bool)).Should().Be(true);

    [Fact]
    public void Convert_Int_FromNumber()
        => ArgumentConverter.Convert(Json("42"), typeof(int)).Should().Be(42);

    [Fact]
    public void Convert_Int_FromString()
        => ArgumentConverter.Convert(Json("\"42\""), typeof(int)).Should().Be(42);

    [Fact]
    public void Convert_Double()
        => ArgumentConverter.Convert(Json("1.5"), typeof(double)).Should().Be(1.5d);

    [Fact]
    public void Convert_Enum_ByName()
        => ArgumentConverter.Convert(Json("\"Green\""), typeof(Color)).Should().Be(Color.Green);

    [Fact]
    public void Convert_Enum_CaseInsensitive()
        => ArgumentConverter.Convert(Json("\"green\""), typeof(Color)).Should().Be(Color.Green);

    [Fact]
    public void Convert_Nullable_Int()
        => ArgumentConverter.Convert(Json("7"), typeof(int?)).Should().Be(7);

    [Fact]
    public void Convert_FileInfo_UsesPath()
    {
        object? result = ArgumentConverter.Convert(Json("\"data/report.txt\""), typeof(FileInfo));

        result.Should().BeOfType<FileInfo>();
        ((FileInfo)result!).Name.Should().Be("report.txt");
    }

    [Fact]
    public void Convert_Array_OfStrings()
    {
        object? result = ArgumentConverter.Convert(Json("[\"a\",\"b\"]"), typeof(string[]));

        result.Should().BeOfType<string[]>();
        ((string[])result!).Should().Equal("a", "b");
    }

    [Fact]
    public void Convert_Array_FromSingleValue_WrapsInArray()
    {
        object? result = ArgumentConverter.Convert(Json("\"only\""), typeof(string[]));

        ((string[])result!).Should().Equal("only");
    }

    [Fact]
    public void Convert_OneOf_StringOrInt_PicksInt()
        => ArgumentConverter.Convert(Json("\"5\""), typeof(OneOf<string, int>))
            .Should().Be((OneOf<string, int>)5);

    [Fact]
    public void Convert_OneOf_StringOrInt_FallsBackToString()
        => ArgumentConverter.Convert(Json("\"abc\""), typeof(OneOf<string, int>))
            .Should().Be((OneOf<string, int>)"abc");
}
