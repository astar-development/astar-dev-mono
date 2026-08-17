namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync;

/// <summary>Creates <see cref="SyncOutcome"/> instances for each terminal sync outcome case.</summary>
public static class SyncOutcomeFactory
{
    /// <summary>Creates the outcome for an account with no configured local sync path.</summary>
    public static SyncOutcome CreateNoSyncPath() => new SyncOutcome.NoSyncPath();

    /// <summary>Creates the outcome for a failed silent token acquisition.</summary>
    public static SyncOutcome CreateAuthFailed(bool reAuthRequired) => new SyncOutcome.AuthFailed(reAuthRequired);

    /// <summary>Creates the outcome for a sync pass that required interactive re-authentication.</summary>
    public static SyncOutcome CreateReAuthRequired() => new SyncOutcome.ReAuthRequired();

    /// <summary>Creates the outcome for an account with no folders selected to sync.</summary>
    public static SyncOutcome CreateNoFoldersSelected() => new SyncOutcome.NoFoldersSelected();

    /// <summary>Creates the outcome for a sync pass that completed with one or more failed jobs.</summary>
    public static SyncOutcome CreateCompletedWithErrors(int failedJobCount) => new SyncOutcome.CompletedWithErrors(failedJobCount);

    /// <summary>Creates the outcome for a sync pass that completed with no failures.</summary>
    public static SyncOutcome CreateCompleted() => new SyncOutcome.Completed();

    /// <summary>Creates the outcome for a cancelled sync pass.</summary>
    public static SyncOutcome CreateCancelled() => new SyncOutcome.Cancelled();

    /// <summary>Creates the outcome for a sync pass that failed with an unexpected exception.</summary>
    public static SyncOutcome CreateUnexpectedError(Exception cause) => new SyncOutcome.UnexpectedError(cause);
}
