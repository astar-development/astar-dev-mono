using System.Collections.Generic;
using System.Linq;
using AStar.Dev.Source.Generators.Attributes;
using Microsoft.CodeAnalysis;

namespace AStar.Dev.Source.Generators.ServiceRegistrationGeneration;

internal sealed class ServiceModel(ServiceLifetime lifetime, string implFqn, string? serviceFqn, bool alsoAsSelf, string @namespace)
{
    public ServiceLifetime Lifetime { get; } = lifetime;
    public string ImplFqn { get; } = implFqn;
    public string? ServiceFqn { get; } = serviceFqn;
    public string? Namespace { get; } = @namespace;
    public bool AlsoAsSelf { get; } = alsoAsSelf;

    public static ServiceModel? TryCreate(INamedTypeSymbol impl, AttributeData attr)
    {
        if(!IsValidImplementationType(impl))
            return null;

        ServiceLifetime lifetime = ExtractLifetime(attr);
        INamedTypeSymbol? asType = ExtractAsType(attr);
        var asSelf = ExtractAsSelf(attr);
        INamedTypeSymbol? service = asType ?? InferServiceType(impl);
        var ns = impl.ContainingNamespace.IsGlobalNamespace ? null : impl.ContainingNamespace.ToDisplayString();

        // Only skip if no service and not alsoAsSelf
        return service is null && !asSelf
            ? null
            : new ServiceModel(
            lifetime: lifetime,
            implFqn: impl.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            serviceFqn: service?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            alsoAsSelf: asSelf,
            @namespace: ns
        );
    }

    private static bool IsValidImplementationType(INamedTypeSymbol impl) => !impl.IsAbstract &&
               impl.Arity == 0 &&
               impl.DeclaredAccessibility == Accessibility.Public;

    private static ServiceLifetime ExtractLifetime(AttributeData attr) => attr.ConstructorArguments.Length == 1 &&
               attr.ConstructorArguments[0].Value is int li
            ? (ServiceLifetime)li
            : ServiceLifetime.Scoped;

    private static INamedTypeSymbol? ExtractAsType(AttributeData attr)
    {
        foreach(KeyValuePair<string, TypedConstant> na in attr.NamedArguments)
        {
            if(na.Key == "As" && na.Value.Value is INamedTypeSymbol ts)
                return ts;
        }

        return null;
    }

    private static bool ExtractAsSelf(AttributeData attr)
    {
        foreach(KeyValuePair<string, TypedConstant> na in attr.NamedArguments)
        {
            if(na.Key == "AsSelf" && na.Value.Value is bool b)
                return b;
        }

        return false;
    }

    private static INamedTypeSymbol? InferServiceType(INamedTypeSymbol impl)
    {
        INamedTypeSymbol[] candidates = [.. impl.AllInterfaces.Where(IsEligibleServiceInterface)];

        return candidates.Length == 1 ? candidates[0] : null;
    }

    private static bool IsEligibleServiceInterface(INamedTypeSymbol i) => i.DeclaredAccessibility == Accessibility.Public &&
               i.TypeKind == TypeKind.Interface &&
               i.Arity == 0 &&
               i.ToDisplayString() != "System.IDisposable";
}
