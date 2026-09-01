using System.Text.Json;
using DevToys.Api;

namespace DevToys.MCP.Tools;

/// <summary>
/// Describes a discovered <see cref="ICommandLineTool"/> as an MCP tool: its exposed name, description,
/// generated input schema, and the option descriptors needed to bind arguments at invocation time.
/// </summary>
internal sealed class CommandLineToolDescriptor
{
    internal CommandLineToolDescriptor(
        ICommandLineTool tool,
        CommandLineToolMetadata metadata,
        string mcpToolName,
        string? description,
        IReadOnlyList<CommandLineOptionDescriptor> options,
        JsonElement inputSchema)
    {
        Guard.IsNotNull(tool);
        Guard.IsNotNull(metadata);
        Guard.IsNotNullOrWhiteSpace(mcpToolName);
        Guard.IsNotNull(options);

        Tool = tool;
        Metadata = metadata;
        McpToolName = mcpToolName;
        Description = description;
        Options = options;
        InputSchema = inputSchema;
    }

    /// <summary>
    /// The discovered command line tool instance.
    /// </summary>
    internal ICommandLineTool Tool { get; }

    /// <summary>
    /// The MEF metadata associated with the tool.
    /// </summary>
    internal CommandLineToolMetadata Metadata { get; }

    /// <summary>
    /// The name under which the tool is exposed to MCP clients (for example "devtoys_base64").
    /// </summary>
    internal string McpToolName { get; }

    /// <summary>
    /// The human readable description of the tool, or <see langword="null"/> if none could be resolved.
    /// </summary>
    internal string? Description { get; }

    /// <summary>
    /// The options of the tool, in declaration order.
    /// </summary>
    internal IReadOnlyList<CommandLineOptionDescriptor> Options { get; }

    /// <summary>
    /// The JSON schema describing the tool's input arguments.
    /// </summary>
    internal JsonElement InputSchema { get; }
}
