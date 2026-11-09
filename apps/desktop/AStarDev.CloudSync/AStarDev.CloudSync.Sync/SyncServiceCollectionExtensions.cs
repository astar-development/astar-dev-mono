using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AStarDev.CloudSync.Sync;

/// <summary>Registers the sync module's in-process services.</summary>
public static class SyncServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IAccountSyncLock"/>, <see cref="IAccountThrottleGate"/> and the keyed <see cref="IProviderCapability{TCapability}"/> resolver as singletons, and <see cref="TimeProvider.System"/> if no clock is registered. The resolver needs an <c>IAccountStore</c>.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddCloudSync(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(typeof(IProviderCapability<>), typeof(KeyedProviderCapability<>));

        return services
            .AddSingleton<IAccountSyncLock, InMemoryAccountSyncLock>()
            .AddSingleton<IAccountThrottleGate, AccountThrottleGate>();
    }
}
