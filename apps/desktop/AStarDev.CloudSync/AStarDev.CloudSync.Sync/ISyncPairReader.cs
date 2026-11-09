using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the sync pairs of an account (#1397). A pair is a view over the selection rules: the top-most Include rule of a subtree.</summary>
public interface ISyncPairReader
{
    /// <summary>Reads the account's pairs, sorted by path ignoring case. A pair has no sync status of its own yet, so it reads as up to date with no files and never synced until the sync engine reports otherwise (#1408).</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The pairs, or the error that stopped the read. A failure is never an empty list.</returns>
    Task<Result<IReadOnlyList<SyncPair>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken);
}
