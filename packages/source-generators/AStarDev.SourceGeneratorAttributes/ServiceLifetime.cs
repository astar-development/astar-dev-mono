namespace AStarDev.SourceGeneratorAttributes;

/// <summary>Specifies the lifetime of a service within a dependency injection container.</summary>
public enum ServiceLifetime
{
    /// <summary>The service will be registered as a <see cref="Singleton"/> service.</summary>
    Singleton,
    /// <summary>The service will be registered as a <see cref="Scoped"/> service.</summary>
    Scoped,
    /// <summary>The service will be registered as a <see cref="Transient"/> service.</summary>
    Transient
}
