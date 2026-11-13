using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Finds what changed on disk under an account's sync root since the sync baseline (#1399). Changes are recomputed on every pass by comparing a scan with the baseline, so a crash never loses one.</summary>
public interface ILocalChangeDetector
{
    /// <summary>Scans the account's sync root once, going only into folders the selection rules include (#1397). A file is hashed only when its size or write time differs from the baseline. A file that changed in the last two seconds is deferred. A file whose write time changed but whose content did not is not reported; its baseline time is refreshed.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">Cancels the scan. A cancelled scan throws <see cref="OperationCanceledException"/> and reports nothing, never a partial set.</param>
    /// <returns>The changes, or why the scan could not run: <see cref="LocalScanFailure.RootUnavailable"/> when the root is missing or unmounted (no change at all is reported, so an unplugged drive never reads as "everything deleted"), or <see cref="LocalScanFailure.Unexpected"/>.</returns>
    Task<Result<LocalChangeSet, LocalScanFailure>> DetectAsync(AccountId accountId, CancellationToken cancellationToken);
}
