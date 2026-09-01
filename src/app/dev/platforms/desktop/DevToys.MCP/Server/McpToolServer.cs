using System.Reflection;
using DevToys.Api;
using DevToys.MCP.Tools;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DevToys.MCP.Server;

/// <summary>
/// Hosts the discovered DevToys command line tools as an MCP server over stdio, answering
/// <c>tools/list</c> and <c>tools/call</c> requests.
/// </summary>
internal sealed partial class McpToolServer
{
    private const string ServerName = "DevToys";

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ListToolsResult _listToolsResult;
    private readonly CommandLineToolInvoker _invoker;

    internal McpToolServer(IReadOnlyList<CommandLineToolDescriptor> tools, ILoggerFactory loggerFactory)
    {
        Guard.IsNotNull(tools);
        Guard.IsNotNull(loggerFactory);

        _logger = this.Log();
        _loggerFactory = loggerFactory;

        IReadOnlyDictionary<string, CommandLineToolDescriptor> toolsByName = BuildToolMap(tools);
        _listToolsResult = BuildListToolsResult(toolsByName);
        _invoker = new CommandLineToolInvoker(toolsByName, loggerFactory);
    }

    internal async Task RunAsync(CancellationToken cancellationToken)
    {
        var options = new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = ServerName,
                Version = GetServerVersion()
            },
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability()
            },
            Handlers = new McpServerHandlers
            {
                ListToolsHandler = (request, token) => new ValueTask<ListToolsResult>(_listToolsResult),
                CallToolHandler = (request, token) => _invoker.InvokeAsync(request.Params, token)
            }
        };

        await using var transport = new StdioServerTransport(options, _loggerFactory);
        await using var server = McpServer.Create(transport, options, _loggerFactory, serviceProvider: null);

        LogServerStarting(_logger, _listToolsResult.Tools.Count);
        await server.RunAsync(cancellationToken);
    }

    private IReadOnlyDictionary<string, CommandLineToolDescriptor> BuildToolMap(IReadOnlyList<CommandLineToolDescriptor> tools)
    {
        var toolsByName = new Dictionary<string, CommandLineToolDescriptor>(StringComparer.Ordinal);
        for (int i = 0; i < tools.Count; i++)
        {
            CommandLineToolDescriptor tool = tools[i];
            if (!toolsByName.TryAdd(tool.McpToolName, tool))
            {
                LogDuplicateToolName(_logger, tool.McpToolName, tool.Metadata.InternalComponentName);
            }
        }

        return toolsByName;
    }

    private static ListToolsResult BuildListToolsResult(IReadOnlyDictionary<string, CommandLineToolDescriptor> toolsByName)
    {
        var tools = new List<Tool>(toolsByName.Count);
        foreach (CommandLineToolDescriptor descriptor in toolsByName.Values)
        {
            tools.Add(new Tool
            {
                Name = descriptor.McpToolName,
                Description = descriptor.Description ?? string.Empty,
                InputSchema = descriptor.InputSchema
            });
        }

        return new ListToolsResult { Tools = tools };
    }

    private static string GetServerVersion()
    {
        Assembly assembly = typeof(McpToolServer).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "1.0.0";
    }

    [LoggerMessage(0, LogLevel.Information, "Starting MCP stdio server with {ToolCount} tool(s).")]
    static partial void LogServerStarting(ILogger logger, int toolCount);

    [LoggerMessage(1, LogLevel.Warning, "Duplicate MCP tool name '{ToolName}' (from '{ComponentName}') was ignored.")]
    static partial void LogDuplicateToolName(ILogger logger, string toolName, string componentName);
}
