using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads which accounts the user paused (#1402).</summary>
public interface IAccountPauseReader
{
    /// <summary>Reads the accounts whose scheduled syncing is paused.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The paused accounts, or the error that stopped the read.</returns>
    Task<Result<IReadOnlyList<AccountId>, ErrorResponse>> GetPausedAsync(CancellationToken cancellationToken);
}
