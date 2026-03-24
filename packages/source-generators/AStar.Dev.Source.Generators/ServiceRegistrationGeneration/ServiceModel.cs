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
        bool asSelf = ExtractAsSelf(attr);
        INamedTypeSymbol? service = asType ?? InferServiceType(impl);
        string ns = (impl.ContainingNamespace.IsGlobalNamespace ? null : impl.ContainingNamespace.ToDisplayString()) ?? string.Empty;

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

    private static bool IsValidImplementationType(INamedTypeSymbol impl) => impl is { IsAbstract: false, Arity: 0, DeclaredAccessibility: Accessibility.Public };

    private static ServiceLifetime ExtractLifetime(AttributeData attr) => attr.ConstructorArguments.Length == 1 &&
               attr.ConstructorArguments[0].Value is int li
            ? (ServiceLifetime)li
            : ServiceLifetime.Scoped;

    private static INamedTypeSymbol? ExtractAsType(AttributeData attr)
    {
        foreach(KeyValuePair<string, TypedConstant> na in attr.NamedArguments)
        {
            if(na is { Key: "As", Value.Value: INamedTypeSymbol ts })
                return ts;
        }

        return null;
    }

    private static bool ExtractAsSelf(AttributeData attr)
    {
        foreach(KeyValuePair<string, TypedConstant> na in attr.NamedArguments)
        {
            if(na is { Key: "AsSelf", Value.Value: bool b })
                return b;
        }

        return false;
    }

    private static INamedTypeSymbol? InferServiceType(INamedTypeSymbol impl)
    {
        INamedTypeSymbol[] candidates = [.. impl.AllInterfaces.Where(IsEligibleServiceInterface)];

        return candidates.Length == 1 ? candidates[0] : null;
    }

    private static bool IsEligibleServiceInterface(INamedTypeSymbol i) => i is { DeclaredAccessibility: Accessibility.Public, TypeKind: TypeKind.Interface, Arity: 0 } &&
                                                                          i.ToDisplayString() != "System.IDisposable";
}
