using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the provider's change feed one page per call (#1398). Providers register it keyed by <c>CloudProvider</c>; consumers get it through <see cref="IProviderCapability{TCapability}"/>.</summary>
public interface IRemoteChangeFeed
{
    /// <summary>Reads one page of changes.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cursor">Where to continue; none for a first full enumeration.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The page, or <see cref="ProviderError.CursorExpired"/> when a full resync is needed.</returns>
    Task<Result<RemoteChangePage, ProviderError>> GetChangesAsync(AccountId accountId, Option<ChangeCursor> cursor, CancellationToken cancellationToken);
}
