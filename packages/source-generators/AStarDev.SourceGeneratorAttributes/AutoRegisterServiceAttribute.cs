namespace AStarDev.SourceGeneratorAttributes;

/// <summary>The <see cref="AutoRegisterServiceAttribute"/> that controls the registration of the service based on the various parameters.</summary>
/// <param name="lifetime">The <see cref="Lifetime"/> to register the service with. If not specified, the default of <see cref="ServiceLifetime.Scoped"/> will be used.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoRegisterServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped) : Attribute
{
    /// <summary>Specifies the lifetime of the service. Defaults to Scoped.</summary>
    public ServiceLifetime Lifetime { get; } = lifetime;

    /// <summary>Override the service interface to register against (optional). When specified, the concrete type will be registered as this type. Otherwise, the generator will use the first listed interface.</summary>
    public Type? As { get; set; }

    /// <summary>Also register the concrete type as itself (optional)</summary>
    public bool AsSelf { get; set; }
}
