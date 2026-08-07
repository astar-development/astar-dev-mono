using Microsoft.CodeAnalysis;

namespace AStarDev.SourceGenerators.StrongIdCodeGeneration;

/// <summary>
///  The <see cref="StrongIdModelExtensions" /> class provides extension methods for working with the <see cref="StrongIdModel" /> class, including a method to create the underlying type from a StrongId attribute.
/// </summary>
public static partial class StrongIdModelExtensions
{
    /// <summary>
    /// Extracts the underlying type from a StrongId attribute, defaulting to System.Guid if not specified.
    /// </summary>
    public static string CreateUnderlyingTypeFromAttribute(AttributeData attr)
    {
        if (attr?.ConstructorArguments.Length != 1)
            return "System.Guid";

        var tc = attr.ConstructorArguments[0];
        if (tc.Kind == TypedConstantKind.Type)
        {
            if (tc.Value is ITypeSymbol typeSymbol)
            {
                string display = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                return display;
            }

            return "System.Guid";
        }

        return tc.Value is string s ? s : tc.Value?.ToString() ?? "System.Guid";
    }
}
