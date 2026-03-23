using System;
using Microsoft.CodeAnalysis;

namespace AStar.Dev.Source.Generators.OptionsBindingGeneration;

public sealed class OptionsTypeInfo : IEquatable<OptionsTypeInfo>
{
    public string TypeName { get; }
    public string FullTypeName { get; }
    public string SectionName { get; }
    public Location Location { get; }

    public OptionsTypeInfo(string typeName, string fullTypeName, string sectionName, Location location)
    {
        TypeName = typeName ?? string.Empty;
        FullTypeName = fullTypeName ?? string.Empty;
        SectionName = sectionName;
        Location = location;
    }

    public override bool Equals(object obj) => Equals((OptionsTypeInfo)obj);

    public bool Equals(OptionsTypeInfo other) => ReferenceEquals(this, other) || (other is not null && string.Equals(TypeName, other.TypeName, System.StringComparison.Ordinal)
            && string.Equals(FullTypeName, other.FullTypeName, System.StringComparison.Ordinal)
            && string.Equals(SectionName, other.SectionName, System.StringComparison.Ordinal)
            && Equals(Location, other.Location));
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 23) + (TypeName != null ? TypeName.GetHashCode() : 0);
            hash = (hash * 23) + (FullTypeName != null ? FullTypeName.GetHashCode() : 0);
            hash = (hash * 23) + (SectionName != null ? SectionName.GetHashCode() : 0);
            hash = (hash * 23) + (Location != null ? Location.GetHashCode() : 0);
            return hash;
        }
    }
    public override string ToString() => $"{FullTypeName} (Section: {SectionName})";
}
