using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.Json;
using DevToys.Api;
using DevToys.Core;
using Microsoft.Extensions.Logging;

namespace DevToys.MCP.Tools;

/// <summary>
/// Discovers the <see cref="ICommandLineTool"/> extensions imported through MEF and turns each one that
/// is supported on the current operating system into a <see cref="CommandLineToolDescriptor"/> exposing an
/// MCP tool name, description, and generated input schema.
/// </summary>
internal sealed partial class CommandLineToolDiscovery
{
    private const string McpToolNamePrefix = "devtoys_";

    private readonly ILogger _logger;

    internal CommandLineToolDiscovery()
    {
        _logger = this.Log();
    }

    internal IReadOnlyList<CommandLineToolDescriptor> DiscoverTools(
        IEnumerable<Lazy<ICommandLineTool, CommandLineToolMetadata>> commandLineTools)
    {
        Guard.IsNotNull(commandLineTools);

        var descriptors = new List<CommandLineToolDescriptor>();

        foreach (Lazy<ICommandLineTool, CommandLineToolMetadata> commandLineTool in commandLineTools)
        {
            CommandLineToolDescriptor? descriptor = TryCreateDescriptor(commandLineTool);
            if (descriptor is not null)
            {
                descriptors.Add(descriptor);
            }
        }

        return descriptors;
    }

    private CommandLineToolDescriptor? TryCreateDescriptor(Lazy<ICommandLineTool, CommandLineToolMetadata> commandLineTool)
    {
        CommandLineToolMetadata metadata = commandLineTool.Metadata;

        // Make sure the tool is supported by the current OS. When no platform is specified by the tool,
        // it is supported by every OS.
        if (!OSHelper.IsOsSupported(metadata.TargetPlatforms))
        {
            LogSkippingUnsupportedTool(_logger, metadata.InternalComponentName);
            return null;
        }

        ICommandLineTool tool = commandLineTool.Value;
        ResourceManager? resourceManager = GetResourceManager(tool, metadata);
        string? description = GetToolDescription(resourceManager, metadata);

        IReadOnlyList<CommandLineOptionDescriptor> options = BuildOptions(tool, resourceManager);
        JsonElement inputSchema = JsonSchemaGenerator.CreateInputSchema(options);

        string mcpToolName = McpToolNamePrefix + SanitizeName(metadata.Name);

        return new CommandLineToolDescriptor(tool, metadata, mcpToolName, description, options, inputSchema);
    }

    private IReadOnlyList<CommandLineOptionDescriptor> BuildOptions(ICommandLineTool tool, ResourceManager? resourceManager)
    {
        var options = new List<CommandLineOptionDescriptor>();

        PropertyInfo[] properties = tool.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        for (int i = 0; i < properties.Length; i++)
        {
            PropertyInfo property = properties[i];
            CommandLineOptionAttribute? attribute = property.GetCustomAttribute<CommandLineOptionAttribute>();
            if (attribute is null)
            {
                continue;
            }

            string name = attribute.Name.Trim('-').ToLowerInvariant();
            string? description = GetOptionDescription(tool, attribute, resourceManager);
            object? defaultValue = property.GetValue(tool);

            options.Add(new CommandLineOptionDescriptor(property, attribute, name, description, defaultValue));
        }

        return options;
    }

    private static string SanitizeName(string name)
    {
        // MCP tool names are restricted to letters, digits, underscores and hyphens. DevToys command names are
        // normally already valid, but any other character is replaced so an odd name can't produce an invalid
        // or ambiguous MCP tool name.
        string lowered = name.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lowered.Length);
        foreach (char c in lowered)
        {
            builder.Append(char.IsAsciiLetterOrDigit(c) || c is '_' or '-' ? c : '_');
        }

        return builder.ToString();
    }

    private static ResourceManager? GetResourceManager(ICommandLineTool commandLineTool, CommandLineToolMetadata metadata)
    {
        return !string.IsNullOrWhiteSpace(metadata.ResourceManagerBaseName)
            ? new ResourceManager(metadata.ResourceManagerBaseName, commandLineTool.GetType().Assembly)
            : null;
    }

    private string? GetToolDescription(ResourceManager? resourceManager, CommandLineToolMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.DescriptionResourceName))
        {
            return null;
        }

        if (resourceManager is null)
        {
            LogGetMetadataStringFailed(_logger, metadata.DescriptionResourceName, metadata.InternalComponentName);
            return null;
        }

        try
        {
            string? description = resourceManager.GetString(metadata.DescriptionResourceName);
            if (description is null)
            {
                LogGetMetadataStringFailed(_logger, metadata.DescriptionResourceName, metadata.InternalComponentName);
            }

            return description;
        }
        catch
        {
            LogGetMetadataStringFailed(_logger, metadata.DescriptionResourceName, metadata.InternalComponentName);
            return null;
        }
    }

    private string? GetOptionDescription(ICommandLineTool commandLineTool, CommandLineOptionAttribute attribute, ResourceManager? parentResourceManager)
    {
        if (string.IsNullOrWhiteSpace(attribute.DescriptionResourceName))
        {
            return null;
        }

        ResourceManager? optionResourceManager
            = !string.IsNullOrWhiteSpace(attribute.ResourceManagerBaseName)
            ? new ResourceManager(attribute.ResourceManagerBaseName, commandLineTool.GetType().Assembly)
            : parentResourceManager;

        string? description = null;
        if (optionResourceManager is not null)
        {
            try
            {
                description = optionResourceManager.GetString(attribute.DescriptionResourceName);
            }
            catch
            {
                description = null;
            }
        }

        if (description is null)
        {
            LogGetOptionMetadataStringFailed(_logger, attribute.DescriptionResourceName);
        }

        return description;
    }

    [LoggerMessage(0, LogLevel.Debug, "Ignoring '{ToolName}' tool as it isn't supported by the current OS.")]
    static partial void LogSkippingUnsupportedTool(ILogger logger, string toolName);

    [LoggerMessage(1, LogLevel.Error, "Unable to get the string for '{MetadataName}' for the tool '{ToolName}'.")]
    static partial void LogGetMetadataStringFailed(ILogger logger, string metadataName, string toolName);

    [LoggerMessage(2, LogLevel.Error, "Unable to get the string for the option '{MetadataName}'.")]
    static partial void LogGetOptionMetadataStringFailed(ILogger logger, string metadataName);
}
