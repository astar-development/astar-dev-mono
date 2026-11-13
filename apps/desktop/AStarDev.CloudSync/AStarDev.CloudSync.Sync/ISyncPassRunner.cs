using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Runs one full sync for an account (#1402). Every trigger goes through it: manual, scheduled (#1403), tray (#1409) and recovery (#1407). It holds the account's sync lease for the whole run, so at most one run per account is in progress; the download and upload passes are only ever called from here.</summary>
public interface ISyncPassRunner
{
    /// <summary>Runs the steps in order: update the remote index, detect local changes, download pass, upload pass. A step that fails stops the run.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="trigger">What started the run.</param>
    /// <param name="cancellationToken">Cancels the run. A cancelled run returns <see cref="SyncPassFailure.Cancelled"/>; it does not throw.</param>
    /// <returns>What the run did, or why it did not finish. <see cref="SyncPassFailure.AlreadyRunning"/> means nothing was started.</returns>
    Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken);
}
