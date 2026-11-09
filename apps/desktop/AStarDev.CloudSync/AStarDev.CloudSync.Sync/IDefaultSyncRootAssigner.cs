using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Gives a newly added account its default sync root (#1394).</summary>
public interface IDefaultSyncRootAssigner
{
    /// <summary>Proposes a default root and saves it. When another account takes a clashing root in between, the proposal is made again.</summary>
    /// <param name="accountId">The new account.</param>
    /// <param name="registration">The account's details.</param>
    /// <param name="cancellationToken">Cancels the assignment.</param>
    /// <returns>The saved root, or why none could be assigned.</returns>
    Task<Result<SyncRoot, SyncRootRejection>> AssignAsync(AccountId accountId, AccountRegistration registration, CancellationToken cancellationToken);
}
