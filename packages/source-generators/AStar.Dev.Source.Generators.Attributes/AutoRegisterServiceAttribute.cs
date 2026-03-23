using System;

namespace AStar.Dev.Source.Generators.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoRegisterServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped) : Attribute
{
    /// <summary>
    /// Specifies the lifetime of the service. Defaults to Scoped.
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;

    /// <summary>
    /// Override the service interface to register against. When specified, the concrete type will be registered as this type. Otherwise, the generator will use the first listed interface.
    /// </summary>
    public Type? As { get; set; }

    /// <summary>
    /// Also register the concrete type as itself (optional)
    /// </summary>
    public bool AsSelf { get; set; } = false;
}
