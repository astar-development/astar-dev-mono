using Microsoft.CodeAnalysis;

namespace AStar.Dev.Source.Generators.OptionsBindingGeneration;

/// <summary>
///  The <see cref="OptionsTypeInfo" /> class represents information about a type that is annotated with the <see cref="Attributes.AutoRegisterOptionsAttribute" />. It contains the type's name, full name, the configuration section name it should bind to, and its location in the source code for diagnostic purposes.
/// </summary>
public sealed class OptionsTypeInfo : IEquatable<OptionsTypeInfo>
{
    /// <summary>
    ///  The <see cref="TypeName" /> property holds the simple name of the type (without namespace).
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// The <see cref="FullTypeName" /> property holds the fully qualified name of the type, including its namespace.
    /// </summary>
    public string FullTypeName { get; }

    /// <summary>
    /// The <see cref="SectionName" /> property holds the name of the configuration section that this options class should bind to. This can be specified via the attribute or by a static SectionName const field on the type.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// The <see cref="Location" /> property holds the location of the type declaration in the source code. This is used for reporting diagnostics if there are issues with the annotated type (e.g., missing section name).
    /// </summary>
    public Location Location { get; }

    /// <summary>
    /// The constructor for <see cref="OptionsTypeInfo" /> initializes all properties. It ensures that TypeName and FullTypeName are not null by defaulting to empty strings if null values are provided. The SectionName and Location are set based on the provided arguments.
    /// </summary>
    /// <param name="typeName"></param>
    /// <param name="fullTypeName"></param>
    /// <param name="sectionName"></param>
    /// <param name="location"></param>
    public OptionsTypeInfo(string typeName, string fullTypeName, string sectionName, Location location)
    {
        TypeName = typeName ?? string.Empty;
        FullTypeName = fullTypeName ?? string.Empty;
        SectionName = sectionName;
        Location = location;
    }

/// <inheritdoc />
    public override bool Equals(object obj) => Equals((OptionsTypeInfo)obj);

/// <inheritdoc />
    public bool Equals(OptionsTypeInfo other) => ReferenceEquals(this, other) || (other is not null && string.Equals(TypeName, other.TypeName, System.StringComparison.Ordinal)
            && string.Equals(FullTypeName, other.FullTypeName, System.StringComparison.Ordinal)
            && string.Equals(SectionName, other.SectionName, System.StringComparison.Ordinal)
            && Equals(Location, other.Location));

/// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + (TypeName != null ? TypeName.GetHashCode() : 0);
            hash = (hash * 23) + (FullTypeName != null ? FullTypeName.GetHashCode() : 0);
            hash = (hash * 23) + (SectionName != null ? SectionName.GetHashCode() : 0);
            hash = (hash * 23) + (Location != null ? Location.GetHashCode() : 0);
            return hash;
        }
    }

/// <inheritdoc />
    public override string ToString() => $"{FullTypeName} (Section: {SectionName})";
}
