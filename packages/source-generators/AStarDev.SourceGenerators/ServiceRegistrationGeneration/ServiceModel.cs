using AStarDev.SourceGeneratorAttributes;
using Microsoft.CodeAnalysis;

namespace AStarDev.SourceGenerators.ServiceRegistrationGeneration;

internal sealed class ServiceModel(ServiceLifetime lifetime, Layer layer, string implFqn, string? serviceFqn, bool alsoAsSelf, string @namespace)
{
    public ServiceLifetime Lifetime { get; } = lifetime;
    public Layer Layer { get; } = layer;
    public string ImplFqn { get; } = implFqn;
    public string? ServiceFqn { get; } = serviceFqn;
    public string? Namespace { get; } = @namespace;
    public bool AlsoAsSelf { get; } = alsoAsSelf;

    public static ServiceModel? TryCreate(INamedTypeSymbol impl, AttributeData attr)
    {
        if (!IsValidImplementationType(impl))
            return null;

        var lifetime = ExtractLifetime(attr);
        var layer = ExtractLayer(attr);
        var asType = ExtractAsType(attr);
        bool asSelf = ExtractAsSelf(attr);
        var service = asType ?? InferServiceType(impl);
        string ns = (impl.ContainingNamespace.IsGlobalNamespace ? null : impl.ContainingNamespace.ToDisplayString()) ?? string.Empty;

        // Only skip if no service and not alsoAsSelf
        return service is null && !asSelf
            ? null
            : new ServiceModel(
            lifetime: lifetime,
            layer: layer,
            implFqn: impl.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            serviceFqn: service?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            alsoAsSelf: asSelf,
            @namespace: ns
        );
    }

    private static bool IsValidImplementationType(INamedTypeSymbol impl) => impl is { IsAbstract: false, Arity: 0, DeclaredAccessibility: Accessibility.Public };

    private static ServiceLifetime ExtractLifetime(AttributeData attr) => attr.ConstructorArguments.Length > 0 &&
               attr.ConstructorArguments[0].Value is int li
            ? (ServiceLifetime)li
            : ServiceLifetime.Scoped;

    private static Layer ExtractLayer(AttributeData attr) => attr.ConstructorArguments.Length > 1 &&
               attr.ConstructorArguments[1].Value is int la
            ? (Layer)la
            : Layer.Miscellaneous;

    private static INamedTypeSymbol? ExtractAsType(AttributeData attr)
    {
        foreach (var na in attr.NamedArguments)
        {
            if (na is { Key: "As", Value.Value: INamedTypeSymbol ts })
                return ts;
        }

        return null;
    }

    private static bool ExtractAsSelf(AttributeData attr)
    {
        foreach (var na in attr.NamedArguments)
        {
            if (na is { Key: "AsSelf", Value.Value: bool b })
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
