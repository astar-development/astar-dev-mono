using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>A module that keeps per-file data for an account's sync root and must delete it when the root changes. Changing the root clears the account's baseline; the sync root store then calls every registered participant.</summary>
public interface ISyncRootChangeParticipant
{
    /// <summary>Deletes the per-file data this module stores for the account. An account it holds nothing for succeeds.</summary>
    /// <param name="accountId">The account whose sync root changed.</param>
    /// <param name="cancellationToken">Cancels the delete.</param>
    /// <returns><see cref="Unit"/> once the data is gone, or the error that stopped it. The participant logs its own failure; the root change stays saved.</returns>
    Task<Result<Unit, ErrorResponse>> RemoveRootDataAsync(AccountId accountId, CancellationToken cancellationToken);
}
