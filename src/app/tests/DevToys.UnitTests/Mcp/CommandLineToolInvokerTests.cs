using System.Text.Json;
using DevToys.MCP.Tools;
using ModelContextProtocol.Protocol;

namespace DevToys.UnitTests.Mcp;

public class CommandLineToolInvokerTests
{
    private static string GetText(CallToolResult result)
        => ((TextContentBlock)result.Content[0]).Text;

    [Fact]
    public void CreateResult_EmptyOutput_Success_SynthesizesSuccessMessage()
    {
        CallToolResult result = CommandLineToolInvoker.CreateResult(string.Empty, string.Empty, 0);

        GetText(result).Should().Be("The tool completed successfully and produced no output.");
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void CreateResult_WhitespaceOnlyOutput_Success_SynthesizesSuccessMessage()
    {
        CallToolResult result = CommandLineToolInvoker.CreateResult("\r\n  \n", string.Empty, 0);

        GetText(result).Should().Be("The tool completed successfully and produced no output.");
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void CreateResult_EmptyOutput_Failure_SynthesizesFailureMessage()
    {
        CallToolResult result = CommandLineToolInvoker.CreateResult(string.Empty, string.Empty, 2);

        GetText(result).Should().Be("The tool failed with exit code 2 and produced no output.");
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void CreateResult_WhitespaceOnlyOutput_Failure_SynthesizesFailureMessage()
    {
        CallToolResult result = CommandLineToolInvoker.CreateResult("   ", string.Empty, 1);

        GetText(result).Should().Be("The tool failed with exit code 1 and produced no output.");
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void CreateResult_RealOutput_KeepsOutput_NoSynthesizedMessage()
    {
        CallToolResult result = CommandLineToolInvoker.CreateResult("actual output", string.Empty, 0);

        GetText(result).Should().Be("actual output");
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void CreateResult_StandardError_IsAppendedToText()
    {
        CallToolResult result = CommandLineToolInvoker.CreateResult("out", "err", 1);

        GetText(result).Should().Be("out" + Environment.NewLine + "err");
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void CreateResult_PreservesRawStreamsInStructuredContent()
    {
        // Whitespace-only stdout is displayed as a synthesized message, but the raw streams must survive
        // untouched in the structured content so callers can still inspect exactly what the tool emitted.
        CallToolResult result = CommandLineToolInvoker.CreateResult("  ", "warning\n", 3);

        var structured = (JsonElement)result.StructuredContent!;
        structured.GetProperty("stdout").GetString().Should().Be("  ");
        structured.GetProperty("stderr").GetString().Should().Be("warning\n");
        structured.GetProperty("exitCode").GetInt32().Should().Be(3);
    }
}
