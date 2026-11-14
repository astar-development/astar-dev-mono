namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncRunOutcome"/> values.</summary>
public static class SyncRunOutcomeFactory
{
    /// <summary>Creates the <see cref="SyncRunOutcome.Completed"/> case.</summary>
    public static SyncRunOutcome CreateCompleted() => new SyncRunOutcome.Completed();

    /// <summary>Creates the <see cref="SyncRunOutcome.Cancelled"/> case.</summary>
    public static SyncRunOutcome CreateCancelled() => new SyncRunOutcome.Cancelled();

    /// <summary>Creates the <see cref="SyncRunOutcome.Failed"/> case.</summary>
    /// <param name="reason">Why the run failed.</param>
    public static SyncRunOutcome CreateFailed(SyncFailureReason reason) => new SyncRunOutcome.Failed(reason);

    /// <summary>Creates the <see cref="SyncRunOutcome.Interrupted"/> case.</summary>
    public static SyncRunOutcome CreateInterrupted() => new SyncRunOutcome.Interrupted();
}
