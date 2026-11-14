using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads how many transfers an account runs at once (#1404). A pass reads it once, at its start.</summary>
public interface ITransferConcurrencyReader
{
    /// <summary>Reads the account's setting.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The setting, which is the default for an account that never changed it, or why it could not be read.</returns>
    Task<Result<TransferConcurrency, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken);
}
