using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Pauses and resumes an account's scheduled syncing (#1402).</summary>
public interface IAccountPauseWriter
{
    /// <summary>Pauses an active account, or resumes a paused one, as one step: two toggles at the same time never both read the same state.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the change.</param>
    /// <returns>The state the account is in now, or the error that stopped the change.</returns>
    Task<Result<PauseState, ErrorResponse>> ToggleAsync(AccountId accountId, CancellationToken cancellationToken);
}
