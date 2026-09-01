using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevToys.Api;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace DevToys.MCP.Tools;

/// <summary>
/// Binds MCP arguments onto a discovered <see cref="ICommandLineTool"/>, invokes it while capturing the
/// console output it writes, and maps the result to an MCP <see cref="CallToolResult"/>.
/// </summary>
/// <remarks>
/// Command line tools write their result straight to <see cref="Console.Out"/>/<see cref="Console.Error"/> and
/// return an exit code, so invocations are serialized with a <see cref="SemaphoreSlim"/>: the console redirect
/// is process-global and only one tool may own it at a time.
/// </remarks>
internal sealed partial class CommandLineToolInvoker
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IReadOnlyDictionary<string, CommandLineToolDescriptor> _toolsByName;
    private readonly SemaphoreSlim _invocationLock = new(1, 1);

    internal CommandLineToolInvoker(IReadOnlyDictionary<string, CommandLineToolDescriptor> toolsByName, ILoggerFactory loggerFactory)
    {
        Guard.IsNotNull(toolsByName);
        Guard.IsNotNull(loggerFactory);

        _logger = this.Log();
        _loggerFactory = loggerFactory;
        _toolsByName = toolsByName;
    }

    internal async ValueTask<CallToolResult> InvokeAsync(CallToolRequestParams? parameters, CancellationToken cancellationToken)
    {
        if (parameters is null || string.IsNullOrEmpty(parameters.Name))
        {
            return CreateErrorResult("No tool name was provided.");
        }

        if (!_toolsByName.TryGetValue(parameters.Name, out CommandLineToolDescriptor? descriptor))
        {
            return CreateErrorResult($"Unknown tool '{parameters.Name}'.");
        }

        await _invocationLock.WaitAsync(cancellationToken);
        try
        {
            return await InvokeCoreAsync(descriptor, parameters, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Let cancellation propagate rather than reporting it as a tool failure.
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(_logger, ex, parameters.Name);
            return CreateErrorResult($"Tool '{parameters.Name}' failed: {ex.Message}");
        }
        finally
        {
            _invocationLock.Release();
        }
    }

    private async ValueTask<CallToolResult> InvokeCoreAsync(
        CommandLineToolDescriptor descriptor,
        CallToolRequestParams parameters,
        CancellationToken cancellationToken)
    {
        BindArguments(descriptor, parameters.Arguments);

        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        using var capturedOut = new StringWriter();
        using var capturedError = new StringWriter();

        int exitCode;
        try
        {
            // Redirect the console so we can capture what the tool writes. The MCP stdio transport writes to
            // the underlying standard output stream directly, so it is unaffected by swapping Console.Out.
            Console.SetOut(capturedOut);
            Console.SetError(capturedError);

            ILogger toolLogger = _loggerFactory.CreateLogger(descriptor.Tool.GetType().FullName!);
            LogInvokingTool(_logger, descriptor.McpToolName);

            exitCode = await descriptor.Tool.InvokeAsync(toolLogger, cancellationToken);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        LogToolExitCode(_logger, descriptor.McpToolName, exitCode);
        return CreateResult(capturedOut.ToString(), capturedError.ToString(), exitCode);
    }

    private static void BindArguments(CommandLineToolDescriptor descriptor, IDictionary<string, JsonElement>? arguments)
    {
        // Every option is (re)assigned on each call because the tool instance is reused across invocations:
        // provided arguments are converted, and omitted ones are reset to the tool's captured default value.
        for (int i = 0; i < descriptor.Options.Count; i++)
        {
            CommandLineOptionDescriptor option = descriptor.Options[i];

            object? value;
            if (arguments is not null
                && arguments.TryGetValue(option.Name, out JsonElement element)
                && element.ValueKind != JsonValueKind.Null)
            {
                value = ArgumentConverter.Convert(element, option.Property.PropertyType);
            }
            else
            {
                value = option.DefaultValue;
            }

            option.Property.SetValue(descriptor.Tool, value);
        }
    }

    internal static CallToolResult CreateResult(string standardOutput, string standardError, int exitCode)
    {
        var text = new StringBuilder();
        text.Append(standardOutput);
        if (!string.IsNullOrEmpty(standardError))
        {
            if (text.Length > 0)
            {
                text.AppendLine();
            }

            text.Append(standardError);
        }

        string combinedOutput = text.ToString();

        // Some tools report their result purely through the exit code without writing anything (or write only
        // blank lines). Treat output that is empty or whitespace-only as "no output" and synthesize a message
        // keyed off the exit code, so a silent success reads as a success and a silent failure isn't surfaced as
        // an opaque empty result.
        if (string.IsNullOrWhiteSpace(combinedOutput))
        {
            combinedOutput = exitCode == 0
                ? "The tool completed successfully and produced no output."
                : $"The tool failed with exit code {exitCode} and produced no output.";
        }

        var structuredContent = new JsonObject
        {
            ["stdout"] = standardOutput,
            ["stderr"] = standardError,
            ["exitCode"] = exitCode
        };

        return new CallToolResult
        {
            Content = { new TextContentBlock { Text = combinedOutput } },
            StructuredContent = JsonSerializer.SerializeToElement(structuredContent),
            IsError = exitCode != 0
        };
    }

    private static CallToolResult CreateErrorResult(string message)
    {
        return new CallToolResult
        {
            Content = { new TextContentBlock { Text = message } },
            IsError = true
        };
    }

    [LoggerMessage(0, LogLevel.Information, "Invoking tool '{ToolName}'...")]
    static partial void LogInvokingTool(ILogger logger, string toolName);

    [LoggerMessage(1, LogLevel.Information, "Tool '{ToolName}' exited with code {ExitCode}.")]
    static partial void LogToolExitCode(ILogger logger, string toolName, int exitCode);

    [LoggerMessage(2, LogLevel.Error, "Tool '{ToolName}' threw an unhandled exception.")]
    static partial void LogInvocationFailed(ILogger logger, Exception exception, string toolName);
}
