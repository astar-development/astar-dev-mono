using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Turns what a sync run did into the status the user sees.</summary>
internal static class SyncPassOutcomeExtensions
{
    /// <summary>The status a run leaves, or none when the run changes nothing the user should see: it was skipped because another run was in progress, or it needs a sign-in, which has its own state.</summary>
    /// <param name="outcome">What the run did.</param>
    internal static Option<SyncStatus> ToStatus(this Result<SyncPassSummary, SyncPassFailure> outcome) =>
        outcome.Match(FromSummary, FromFailure);

    private static Option<SyncStatus> FromSummary(SyncPassSummary summary) =>
        Option.Some(summary.Conflicts + summary.HeldDeletions > 0 ? SyncStatusFactory.CreateNeedsAttention(summary.Conflicts, summary.HeldDeletions) : SyncStatusFactory.CreateUpToDate());

    private static Option<SyncStatus> FromFailure(SyncPassFailure failure) => failure switch
    {
        SyncPassFailure.AlreadyRunning or SyncPassFailure.AuthenticationRequired => Option.None<SyncStatus>(),
        SyncPassFailure.Cancelled => Option.Some(SyncStatusFactory.CreateCancelled()),
        SyncPassFailure.RootUnavailable => Failed(SyncFailureReasonFactory.CreateRootUnavailable()),
        SyncPassFailure.QuotaExceeded => Failed(SyncFailureReasonFactory.CreateQuotaExceeded()),
        SyncPassFailure.InsufficientDiskSpace => Failed(SyncFailureReasonFactory.CreateInsufficientDiskSpace()),
        _ => Failed(SyncFailureReasonFactory.CreateUnexpected()),
    };

    private static Option<SyncStatus> Failed(SyncFailureReason reason) => Option.Some(SyncStatusFactory.CreateFailed(reason));
}
