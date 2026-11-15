using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads an account's sync baseline: the last state both sides agreed on (#1399). A scan reads it in pages, so memory stays bounded for hundreds of thousands of items.</summary>
public interface ISyncBaselineReader
{
    /// <summary>Reads one item.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The item's path below the sync root.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The entry, none when the path was never synced, or the error that stopped the read.</returns>
    Task<Result<Option<SyncBaselineEntry>, ErrorResponse>> GetAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken);

    /// <summary>Reads one page of the items at or below a folder, sorted by path ignoring case. Call again with the last path of the page as <paramref name="after"/> until a page comes back shorter than <paramref name="limit"/>.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="under">The folder; the item itself and everything below it. The empty path reads the whole account.</param>
    /// <param name="after">Skip every path up to and including this one; none for the first page.</param>
    /// <param name="limit">The most entries to return; below one it becomes one and above 5000 it becomes 5000.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The page, or the error that stopped the read. A failure is never an empty or short page: a stored row that is invalid fails the whole read.</returns>
    Task<Result<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>> GetUnderAsync(AccountId accountId, RelativeSyncPath under, Option<RelativeSyncPath> after, int limit, CancellationToken cancellationToken);

    /// <summary>Reads every baseline row of one cloud item, by its id and not by a scan (#1626). An item is normally at one path, but a half-finished move can leave two.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="itemId">The cloud item.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rows sorted by path ignoring case; empty when the item has none; or the error that stopped the read.</returns>
    Task<Result<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>> GetByItemAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken);
}
