using System;

namespace AStar.Dev.Source.Generators.Attributes;

/// <summary>
/// Attribute used to indicate that a class or struct should be automatically registered as an <see cref="IOptions{T}"/>,
/// with an optional section name provided for configuration purposes.
/// </summary>
/// <remarks>
/// This attribute can only be applied to classes or structs. It is not inherited by derived types
/// and does not allow multiple usage on the same target.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class AutoRegisterOptionsAttribute(string? sectionName = null) : Attribute
{
    /// <summary>
    /// Gets the name of the configuration section associated with this instance.
    /// When not set, the section name defaults to the class or struct name.
    /// </summary>
    public string? SectionName { get; } = sectionName;
}
