using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AStarDev.CloudSync.Sync;

/// <summary>Registers the sync module's in-process services.</summary>
public static class SyncServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IAccountSyncLock"/>, <see cref="IAccountThrottleGate"/> the keyed <see cref="IProviderCapability{TCapability}"/> resolver and the account services (<see cref="IAccountActions"/>, <see cref="IAccountRemover"/>, <see cref="IAccountSignInStates"/>, <see cref="ISignInStateRefresher"/>) as singletons, and <see cref="TimeProvider.System"/> if no clock is registered. Needs an <c>IAccountStore</c> and logging (<c>AddLogging</c>).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddCloudSync(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(typeof(IProviderCapability<>), typeof(KeyedProviderCapability<>));

        return services
            .AddSingleton<IAccountSyncLock, InMemoryAccountSyncLock>()
            .AddSingleton<IAccountThrottleGate, AccountThrottleGate>()
            .AddSingleton<IAccountSignInStates, InMemoryAccountSignInStates>()
            .AddSingleton<AccountLeaseWaiter>()
            .AddSingleton<IAccountRemover, AccountRemover>()
            .AddSingleton<IAccountActions, AccountActions>()
            .AddSingleton<ISignInStateRefresher, SignInStateRefresher>();
    }
}
