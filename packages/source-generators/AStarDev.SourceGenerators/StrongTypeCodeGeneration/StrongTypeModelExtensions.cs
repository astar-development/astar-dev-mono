using Microsoft.CodeAnalysis;

namespace AStarDev.SourceGenerators.StrongTypeCodeGeneration;

/// <summary>
///  The <see cref="StrongTypeModelExtensions" /> class provides extension methods for working with the <see cref="StrongTypeModel" /> class, including a method to create the underlying type from a StrongType attribute.
/// </summary>
public static partial class StrongTypeModelExtensions
{
    /// <summary>Extracts the underlying type from a StrongType attribute, defaulting to System.Guid if not specified.</summary>
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
