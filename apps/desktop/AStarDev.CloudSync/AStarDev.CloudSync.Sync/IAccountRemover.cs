using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Removes an account: waits for its sync to stop, signs it out, deletes its data in every module, then forgets it. Local files are never deleted.</summary>
public interface IAccountRemover
{
    /// <summary>Removes the account.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the removal before anything is deleted.</param>
    Task<Result<Unit, AccountRemovalFailure>> RemoveAsync(AccountId accountId, CancellationToken cancellationToken);
}
