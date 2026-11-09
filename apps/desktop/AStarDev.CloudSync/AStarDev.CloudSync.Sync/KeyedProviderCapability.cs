using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Resolves capabilities registered as keyed services keyed by <see cref="CloudProvider"/>.</summary>
/// <typeparam name="TCapability">The capability interface.</typeparam>
/// <param name="services">The container holding the keyed registrations.</param>
/// <param name="accountStore">Maps an account to its provider.</param>
/// <param name="logger">Reports account reads that fail, without account details.</param>
internal sealed class KeyedProviderCapability<TCapability>(IServiceProvider services, IAccountStore accountStore, ILogger<KeyedProviderCapability<TCapability>> logger) : IProviderCapability<TCapability>
    where TCapability : class
{
    private const string Location = "KeyedProviderCapability";

    /// <inheritdoc />
    public Result<TCapability, ProviderError> ForProvider(CloudProvider provider) =>
        services.GetKeyedService<TCapability>(provider) is { } capability
            ? Result.Success<TCapability, ProviderError>(capability)
            : Result.Failure<TCapability, ProviderError>(ProviderErrorFactory.CreateUnsupported());

    /// <inheritdoc />
    public Task<Result<TCapability, ProviderError>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken) =>
        accountStore.GetAllAsync(cancellationToken)
            .MatchAsync(
                accounts => accounts.FirstOrNone(account => account.Id == accountId).Match(account => ForProvider(account.Registration.Provider), () => Result.Failure<TCapability, ProviderError>(ProviderErrorFactory.CreateNotFound())),
                AccountsUnreadable);

    private Result<TCapability, ProviderError> AccountsUnreadable(ErrorResponse error)
    {
        _ = logger;

        return Result.Failure<TCapability, ProviderError>(ProviderErrorFactory.CreateTransient());
    }
}
