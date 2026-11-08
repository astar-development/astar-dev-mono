using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts;

/// <summary>A module that keeps data for an account and must delete it when the account is removed. The remove-account flow calls every registered participant.</summary>
public interface IAccountRemovalParticipant
{
    /// <summary>Deletes everything this module stores for the account. Deleting an account it holds nothing for succeeds.</summary>
    /// <param name="accountId">The account being removed.</param>
    /// <param name="cancellationToken">Cancels the removal.</param>
    /// <returns><see cref="Unit"/> once the data is gone, or the error that stopped it, which fails the removal.</returns>
    Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken);
}
