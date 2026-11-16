using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Classification;

internal static class UndecoratedRegistration
{
    internal const string Key = "undecorated";

    internal static IServiceCollection MoveToUndecoratedKey<TService>(this IServiceCollection services)
        where TService : class
    {
        var previous = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(TService) && !descriptor.IsKeyedService);
        if (previous is null)
            return services;

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
