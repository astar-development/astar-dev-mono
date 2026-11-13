using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Writes an account's sync baseline. The download and upload engines (#1400, #1401) call it after a transfer succeeds; the local scan refreshes the time of a touched-only file (#1399).</summary>
public interface ISyncBaselineWriter
{
    /// <summary>Adds or replaces entries in one transaction. Two entries for the same path ignoring case: the last wins.</summary>
    /// <param name="accountId">The account the entries belong to; each entry's own account is ignored.</param>
    /// <param name="entries">The entries.</param>
    /// <param name="cancellationToken">Cancels the write; nothing is saved.</param>
    /// <returns>Success, or the error that stopped the write.</returns>
    Task<Result<Unit, ErrorResponse>> UpsertAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken);

    /// <summary>Removes entries in one transaction. A path with no entry is ignored.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="paths">The paths to remove.</param>
    /// <param name="cancellationToken">Cancels the write; nothing is removed.</param>
    /// <returns>Success, or the error that stopped the write.</returns>
    Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IReadOnlyList<RelativeSyncPath> paths, CancellationToken cancellationToken);
}
