using System.Text;
using DevToys.Api;
using DevToys.Core;
using DevToys.Core.Logging;
using DevToys.Core.Mef;
using DevToys.MCP.Core;
using DevToys.MCP.Server;
using DevToys.MCP.Tools;
using Microsoft.Extensions.Logging;

namespace DevToys.MCP;

internal static partial class Program
{
    private static async Task Main()
    {
        // Enable support for multiple encodings, especially in .NET Core.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using ILoggerFactory loggerFactory = CreateLoggerFactory();
        LoggingExtensions.LoggerFactory = loggerFactory;
        ILogger logger = loggerFactory.CreateLogger(typeof(Program).FullName!);

        try
        {
            FileHelper.ClearTempFiles(Constants.AppTempFolder);

            // Initialize MEF and discover the command line tools shipped as DevToys extensions
            // (found in the "Plugins" folder next to the executable, or through the EXTRAPLUGIN
            // environment variable, exactly like DevToys CLI does).
            using var mefComposer
                = new MefComposer(
                    assemblies: new[] { typeof(Program).Assembly },
                    pluginFolders: new[] { Path.Combine(AppContext.BaseDirectory, "Plugins") });

            IEnumerable<Lazy<ICommandLineTool, CommandLineToolMetadata>> commandLineTools
                = mefComposer.Provider.ImportMany<ICommandLineTool, CommandLineToolMetadata>();

            // Turn each command line tool into a descriptor exposing an MCP tool name and input schema.
            var toolDiscovery = new CommandLineToolDiscovery();
            IReadOnlyList<CommandLineToolDescriptor> tools = toolDiscovery.DiscoverTools(commandLineTools);

            LogDiscoveredToolCount(logger, tools.Count);
            foreach (CommandLineToolDescriptor tool in tools)
            {
                LogDiscoveredTool(logger, tool.McpToolName, tool.Options.Count);
            }

            // Expose the discovered tools as an MCP server over stdio. This runs until the client disconnects.
            var server = new McpToolServer(tools, loggerFactory);
            await server.RunAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogUnhandledException(logger, ex);
        }
        finally
        {
            FileHelper.ClearTempFiles(Constants.AppTempFolder);
        }
    }

    private static ILoggerFactory CreateLoggerFactory()
    {
        // The standard output is reserved for the MCP protocol, so logs must only go to disk (and the
        // debugger). Never add a console logger here.
        return LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFile(new FileStorage());

#if DEBUG
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Trace);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif
        });
    }

    [LoggerMessage(0, LogLevel.Information, "Discovered {ToolCount} command line tool(s).")]
    static partial void LogDiscoveredToolCount(ILogger logger, int toolCount);

    [LoggerMessage(2, LogLevel.Debug, "Exposed MCP tool '{ToolName}' with {OptionCount} option(s).")]
    static partial void LogDiscoveredTool(ILogger logger, string toolName, int optionCount);

    [LoggerMessage(1, LogLevel.Critical, "Unhandled exception during startup.")]
    static partial void LogUnhandledException(ILogger logger, Exception exception);
}
