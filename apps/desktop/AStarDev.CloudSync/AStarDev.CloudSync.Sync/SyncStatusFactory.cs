using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncStatus"/> cases.</summary>
public static class SyncStatusFactory
{
    /// <summary>Creates a syncing status. The percentage is clamped to 0–100.</summary>
    public static SyncStatus CreateSyncing(int percentComplete) => new SyncStatus.Syncing(Math.Clamp(percentComplete, 0, 100));

    /// <summary>Creates an up-to-date status.</summary>
    public static SyncStatus CreateUpToDate() => new SyncStatus.UpToDate();

    /// <summary>Creates a conflicts status. A count below 1 is treated as 1.</summary>
    public static SyncStatus CreateHasConflicts(int count) => new SyncStatus.HasConflicts(Math.Max(count, 1));

    /// <summary>Creates a paused status.</summary>
    public static SyncStatus CreatePaused() => new SyncStatus.Paused();

    /// <summary>Creates a failed status.</summary>
    /// <param name="reason">Why the run did not finish.</param>
    public static SyncStatus CreateFailed(SyncFailureReason reason) => new SyncStatus.Failed(reason);

    /// <summary>Creates a needs-attention status. Negative counts become zero; when both are zero the status is up to date.</summary>
    /// <param name="conflicts">Paths that changed on both sides.</param>
    /// <param name="heldDeletions">Deletions the mass-deletion guard held.</param>
    public static SyncStatus CreateNeedsAttention(int conflicts, int heldDeletions) =>
        conflicts <= 0 && heldDeletions <= 0 ? CreateUpToDate() : new SyncStatus.NeedsAttention(Math.Max(conflicts, 0), Math.Max(heldDeletions, 0));

    /// <summary>Creates the status an account shows after a sync run ended: up to date, needs attention, or failed with the reason.</summary>
    /// <param name="outcome">What the run did.</param>
    public static SyncStatus CreateFromRun(Result<SyncPassSummary, SyncPassFailure> outcome) =>
        outcome.Match(summary => CreateNeedsAttention(summary.Conflicts, summary.HeldDeletions), failure => CreateFailed(ReasonOf(failure)));

    private static SyncFailureReason ReasonOf(SyncPassFailure failure) => failure switch
    {
        SyncPassFailure.AlreadyRunning => SyncFailureReasonFactory.CreateCouldNotStart(),
        SyncPassFailure.AuthenticationRequired => SyncFailureReasonFactory.CreateAuthenticationRequired(),
        SyncPassFailure.RootUnavailable => SyncFailureReasonFactory.CreateRootUnavailable(),
        SyncPassFailure.QuotaExceeded => SyncFailureReasonFactory.CreateQuotaExceeded(),
        SyncPassFailure.InsufficientDiskSpace => SyncFailureReasonFactory.CreateInsufficientDiskSpace(),
        SyncPassFailure.Cancelled => SyncFailureReasonFactory.CreateCancelled(),
        _ => SyncFailureReasonFactory.CreateUnexpected(),
    };
}
