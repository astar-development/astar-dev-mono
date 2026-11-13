using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Brings an account's remote item index up to date from the provider's change feed (#1398). It is provider-neutral: it reads the feed through <see cref="IRemoteChangeFeed"/>.</summary>
public interface IRemoteIndexUpdater
{
    /// <summary>Reads pages until the feed is caught up, applying each page and its cursor. A resync after <see cref="ProviderError.CursorExpired"/> never deletes anything on disk. Only one update per account runs at a time; a second request waits.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the update between and during pages; the last saved cursor is kept.</param>
    /// <returns>What changed, or the provider error that stopped the update.</returns>
    Task<Result<RemoteUpdateSummary, ProviderError>> UpdateAsync(AccountId accountId, CancellationToken cancellationToken);
}
