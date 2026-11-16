using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Classification;

/// <summary>Moves an earlier registration to a private key so a decorator can wrap it without resolving itself.</summary>
internal static class UndecoratedRegistration
{
    internal const string Key = "undecorated";

    /// <summary>Re-registers the last unkeyed registration of <typeparamref name="TService"/> under <see cref="Key"/>. This runs while the container is composed, so a wiring mistake surfaces at start-up rather than as a failure on first resolve.</summary>
    /// <typeparam name="TService">The service to move.</typeparam>
    /// <param name="services">The service collection to change.</param>
    /// <returns>The <paramref name="services" /> collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TService"/> was already moved, which means <c>AddClassification</c> was called twice, or when no earlier registration exists to move.</exception>
    internal static IServiceCollection MoveToUndecoratedKey<TService>(this IServiceCollection services)
        where TService : class
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TService) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, Key)))
            throw new InvalidOperationException($"{typeof(TService).Name} was already moved to the undecorated key: AddClassification must be called once.");

        var previous = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(TService) && !descriptor.IsKeyedService)
            ?? throw new InvalidOperationException($"No {typeof(TService).Name} is registered to decorate: register the sync and classification persistence before AddClassification.");

        _ = services.Remove(previous);
        services.Add(Keyed(previous));

        return services;
    }

    private static ServiceDescriptor Keyed(ServiceDescriptor previous)
    {
        if (previous.ImplementationInstance is not null)
            return new ServiceDescriptor(previous.ServiceType, Key, previous.ImplementationInstance);

        if (previous.ImplementationFactory is not null)
        {
            var factory = previous.ImplementationFactory;

            return new ServiceDescriptor(previous.ServiceType, Key, (provider, _) => factory(provider), previous.Lifetime);
        }

        return new ServiceDescriptor(previous.ServiceType, Key, previous.ImplementationType!, previous.Lifetime);
    }
}
