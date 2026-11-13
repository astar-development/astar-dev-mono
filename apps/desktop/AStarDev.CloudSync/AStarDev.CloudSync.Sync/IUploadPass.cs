using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>The local-to-remote half of a sync run: plans the local changes against the baseline and the remote index, then brings the provider up to date. The caller holds the account's sync lease for the whole run and runs this after the download pass.</summary>
public interface IUploadPass
{
    /// <summary>Runs the pass.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="changes">What the local scan found.</param>
    /// <param name="cancellationToken">Cancels the pass: no new upload starts, in-flight uploads are cancelled and their sessions deleted, and the baseline rows already written are kept.</param>
    /// <returns>What the pass did, or why it stopped. One file failing never fails the pass.</returns>
    Task<Result<UploadPassSummary, UploadPassFailure>> RunAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken);
}
