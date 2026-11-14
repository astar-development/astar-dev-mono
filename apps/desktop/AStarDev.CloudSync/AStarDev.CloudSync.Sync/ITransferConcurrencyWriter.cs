using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Changes how many transfers an account runs at once (#1404). It applies from the account's next pass.</summary>
public interface ITransferConcurrencyWriter
{
    /// <summary>Saves the account's setting.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="concurrency">The new setting.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<Result<Unit, ErrorResponse>> SetAsync(AccountId accountId, TransferConcurrency concurrency, CancellationToken cancellationToken);
}
