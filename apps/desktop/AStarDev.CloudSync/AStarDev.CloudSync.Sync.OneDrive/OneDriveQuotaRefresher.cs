using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <inheritdoc />
/// <param name="graphAccess">Gives the account's drive id, access token and Graph client.</param>
/// <param name="timeProvider">Turns a Retry-After date into a delay.</param>
/// <param name="logger">Reports the outcome with the hashed account id, status and Graph error code only.</param>
internal sealed class OneDriveQuotaRefresher(OneDriveGraphAccess graphAccess, TimeProvider timeProvider, ILogger<OneDriveQuotaRefresher> logger) : IQuotaRefresher
{
    /// <inheritdoc />
    public Task<Result<StorageUsage, ProviderError>> RefreshAsync(AccountId accountId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{graphAccess} {timeProvider} {logger} {accountId} {cancellationToken}");
}
