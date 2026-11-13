using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads an account's remote item index (#1398). The index is metadata only, keyed by item id; paths are computed from the parent chain and never stored.</summary>
public interface IRemoteIndexReader
{
    /// <summary>Reads the saved change cursor.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The cursor, none before the first page was applied, or the error that stopped the read.</returns>
    Task<Result<Option<ChangeCursor>, ErrorResponse>> GetCursorAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Reads the resync pass that is running, so a crash in a resync resumes as a resync (#1459).</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The pass, none when no resync is running, or the error that stopped the read.</returns>
    Task<Result<Option<ResyncPassId>, ErrorResponse>> GetResyncPassAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Reads one item. A deleted item is returned with <see cref="RemoteItemKind.Deleted"/>, so the engine can see the deletion.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="itemId">The remote item id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The item, none when it was never indexed, or the error that stopped the read.</returns>
    Task<Result<Option<RemoteItemSnapshot>, ErrorResponse>> GetAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken);

    /// <summary>Reads the live children of a folder, sorted by name ignoring case. Deleted items are left out.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="parent">The folder; none for the top level of the drive.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The children, or the error that stopped the read. A failure is never an empty list.</returns>
    Task<Result<IReadOnlyList<RemoteItemSnapshot>, ErrorResponse>> GetChildrenAsync(AccountId accountId, Option<RemoteItemId> parent, CancellationToken cancellationToken);

    /// <summary>Computes an item's path below the drive root from its parent chain.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="itemId">The remote item id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The path, none when the item is not indexed or its chain loops, or the error that stopped the read.</returns>
    Task<Result<Option<LocalRelativePath>, ErrorResponse>> GetPathAsync(AccountId accountId, RemoteItemId itemId, CancellationToken cancellationToken);

    /// <summary>Tells whether the index holds the whole drive: the first full enumeration has reached its last page (#1540). A cursor alone does not say so, because the cursor is saved after every page.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>True once a caught-up page was saved, or the error that stopped the read. A failure is never read as complete.</returns>
    Task<Result<bool, ErrorResponse>> IsCompleteAsync(AccountId accountId, CancellationToken cancellationToken);
}
