using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="refreshers">Finds the provider's quota reader.</param>
/// <param name="accountStore">Saves the usage.</param>
/// <param name="logger">Reports a refresh that failed, by error type only.</param>
internal sealed class AccountQuotaRefresher(IProviderCapability<IQuotaRefresher> refreshers, IAccountStore accountStore, ILogger<AccountQuotaRefresher> logger) : IAccountQuotaRefresher
{
    private const string Location = nameof(AccountQuotaRefresher);

    /// <inheritdoc />
    public Task<Result<StorageUsage, ProviderError>> RefreshAsync(AccountId accountId, CancellationToken cancellationToken) =>
        refreshers.ForAccountAsync(accountId, cancellationToken)
            .BindAsync(refresher => refresher.RefreshAsync(accountId, cancellationToken))
            .BindAsync(usage => SaveAsync(accountId, usage, cancellationToken))
            .MatchAsync(Result.Success<StorageUsage, ProviderError>, Failed);

    private Task<Result<StorageUsage, ProviderError>> SaveAsync(AccountId accountId, StorageUsage usage, CancellationToken cancellationToken) =>
        accountStore.UpdateStorageAsync(accountId, usage, cancellationToken)
            .MatchAsync(_ => Result.Success<StorageUsage, ProviderError>(usage), _ => Result.Failure<StorageUsage, ProviderError>(ProviderErrorFactory.CreateTransient()));

    private Result<StorageUsage, ProviderError> Failed(ProviderError error)
    {
        LogMessage.Warning(logger, Location, $"The storage quota was not refreshed, so the last known value stays ({error.GetType().Name}).");

        return Result.Failure<StorageUsage, ProviderError>(error);
    }
}
