using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Changes an account's remote item index (#1398). Every call is one transaction.</summary>
public interface IRemoteIndexWriter
{
    /// <summary>Applies one page and saves its cursor in the same transaction. The last occurrence of an item in the page wins. Applying the same page again changes nothing.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="page">The page.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>What changed, or the error that stopped the write; on an error nothing was saved, and the cursor is unchanged.</returns>
    Task<Result<RemoteIndexDelta, ErrorResponse>> ApplyPageAsync(AccountId accountId, RemoteChangePage page, CancellationToken cancellationToken);

    /// <summary>Starts a resync: every indexed item counts as unseen until a page returns it. Starting again while a resync runs resumes that pass.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The pass that is running, or the error that stopped the write.</returns>
    Task<Result<ResyncPassId, ErrorResponse>> BeginResyncAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Finishes the resync: items no page returned are marked deleted, and the pass ends. Nothing is deleted on disk; the changes go to the deletion and conflict path (#1405).</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The missing items as deleted changes, or the error that stopped the write.</returns>
    Task<Result<RemoteIndexDelta, ErrorResponse>> CompleteResyncAsync(AccountId accountId, CancellationToken cancellationToken);
}
