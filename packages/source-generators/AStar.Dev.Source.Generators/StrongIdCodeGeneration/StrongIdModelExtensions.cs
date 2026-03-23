using Microsoft.CodeAnalysis;

namespace AStar.Dev.Source.Generators.StrongIdCodeGeneration;

public static partial class StrongIdModelExtensions
{
    /// <summary>
    /// Extracts the underlying type from a StrongId attribute, defaulting to System.Guid if not specified.
    /// </summary>
    public static string CreateUnderlyingTypeFromAttribute(AttributeData attr)
    {
        if(attr.ConstructorArguments.Length != 1)
            return "System.Guid";

        TypedConstant tc = attr.ConstructorArguments[0];
        if(tc.Kind == TypedConstantKind.Type)
        {
            if(tc.Value is ITypeSymbol typeSymbol)
            {
                // Prefer a fully-qualified display (remove the global:: if present)
                var display = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                return display;
            }

            if(tc.Type is not null)
                return tc.Type.ToDisplayString();
        }

        return tc.Value is string s ? s : tc.Value?.ToString() ?? "System.Guid";
    }
}
