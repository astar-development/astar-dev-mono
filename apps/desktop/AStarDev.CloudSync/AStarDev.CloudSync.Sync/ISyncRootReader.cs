using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the sync root each account syncs into (#1394).</summary>
public interface ISyncRootReader
{
    /// <summary>Reads the account's sync root.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The root, none when the account has no root yet, or the error that stopped the read.</returns>
    Task<Result<Option<SyncRoot>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Reads every account's sync root.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The roots, or the error that stopped the read.</returns>
    Task<Result<IReadOnlyList<AccountSyncRoot>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken);
}
