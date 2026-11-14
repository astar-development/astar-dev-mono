using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Settles a set of deletions the mass-deletion guard holds (#1406). It holds the account's sync lease for the whole call.</summary>
public interface IHeldDeletionDecider
{
    /// <summary>Runs the held deletions: deleted-in-the-cloud files go to the sync trash, deleted-locally files are removed in the cloud. The set is closed only when every path was done.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="id">The held set.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or why the set stays open.</returns>
    Task<Result<Unit, ErrorResponse>> ConfirmAsync(AccountId accountId, HeldDeletionId id, CancellationToken cancellationToken);

    /// <summary>Undoes the held deletions: files deleted locally are downloaded again, files deleted in the cloud are uploaded again. The set is closed only when every path was done.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="id">The held set.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or why the set stays open.</returns>
    Task<Result<Unit, ErrorResponse>> RestoreAsync(AccountId accountId, HeldDeletionId id, CancellationToken cancellationToken);
}
