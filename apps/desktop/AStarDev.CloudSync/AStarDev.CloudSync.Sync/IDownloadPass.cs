using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The remote-to-local half of a sync run for one account (#1400): it plans against the remote index, the baseline and the local scan, then downloads, creates folders, moves and deletes.</summary>
public interface IDownloadPass
{
    /// <summary>Runs the pass. The caller holds the account's sync lease and has updated the remote index and scanned the local side. One file failing does not fail the pass; it is counted in the summary.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="localChanges">The local scan result for this run.</param>
    /// <param name="cancellationToken">Cancels the pass: no new download starts, running ones stop and delete their temp files, and the baseline rows already written are kept.</param>
    /// <returns>
    /// What the pass did, or why it stopped. <see cref="DownloadPassFailure.InsufficientDiskSpace"/> means the whole sync run must stop: the caller must not start the upload pass.
    /// </returns>
    Task<Result<DownloadPassSummary, DownloadPassFailure>> RunAsync(AccountId accountId, LocalChangeSet localChanges, CancellationToken cancellationToken);
}
