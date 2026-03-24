using Microsoft.CodeAnalysis;

namespace AStar.Dev.Source.Generators.StrongIdCodeGeneration;

internal sealed class StrongIdModel(string? namepsace, string modelName, Accessibility accessibility, string underlyingTypeDisplay)
{
    public string? Namespace { get; } = namepsace;
    public string ModelName { get; } = modelName;
    public Accessibility Accessibility { get; } = accessibility;
    public string UnderlyingTypeDisplay { get; } = underlyingTypeDisplay;

    public static bool Equals(StrongIdModel? x, StrongIdModel? y)
        => ReferenceEquals(x, y) || (x is not null && y is not null && string.Equals(x.Namespace, y.Namespace, StringComparison.Ordinal) &&
               string.Equals(x.ModelName, y.ModelName, StringComparison.Ordinal) &&
               string.Equals(x.UnderlyingTypeDisplay, y.UnderlyingTypeDisplay, StringComparison.Ordinal) &&
               x.Accessibility == y.Accessibility);

    public override bool Equals(object? obj) => obj is StrongIdModel other && Equals(this, other);
    public override int GetHashCode() => GetHashCode(this);
    public static int GetHashCode(StrongIdModel obj) => (obj.Namespace, obj.ModelName, obj.UnderlyingTypeDisplay, obj.Accessibility).GetHashCode();
}
