using System.Reflection;
using DevToys.Api;

namespace DevToys.MCP.Tools;

/// <summary>
/// Describes a single <see cref="CommandLineOptionAttribute"/>-decorated property of an
/// <see cref="ICommandLineTool"/>, resolved into the shape needed to build an MCP tool input schema
/// and, later, to bind incoming argument values back onto the tool instance.
/// </summary>
internal sealed class CommandLineOptionDescriptor
{
    internal CommandLineOptionDescriptor(
        PropertyInfo property,
        CommandLineOptionAttribute attribute,
        string name,
        string? description,
        object? defaultValue)
    {
        Guard.IsNotNull(property);
        Guard.IsNotNull(attribute);
        Guard.IsNotNullOrWhiteSpace(name);

        Property = property;
        Attribute = attribute;
        Name = name;
        Description = description;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// The property backing this option on the command line tool instance.
    /// </summary>
    internal PropertyInfo Property { get; }

    /// <summary>
    /// The attribute that declared this option.
    /// </summary>
    internal CommandLineOptionAttribute Attribute { get; }

    /// <summary>
    /// The normalized argument key exposed to MCP clients (for example "file"), without the leading dashes
    /// used on the command line.
    /// </summary>
    internal string Name { get; }

    /// <summary>
    /// The human readable description of the option, or <see langword="null"/> if none could be resolved.
    /// </summary>
    internal string? Description { get; }

    /// <summary>
    /// The default value read from the tool instance, or <see langword="null"/> if none was set.
    /// </summary>
    internal object? DefaultValue { get; }

    /// <summary>
    /// Whether the option must be provided by the caller.
    /// </summary>
    internal bool IsRequired => Attribute.IsRequired;
}
