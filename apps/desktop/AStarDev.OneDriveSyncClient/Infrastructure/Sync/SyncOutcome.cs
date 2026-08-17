namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync;

/// <summary>Base type for the terminal outcomes of a single account sync pass.</summary>
public abstract record SyncOutcome
{
    /// <summary>The account has no configured local sync path.</summary>
    public sealed record NoSyncPath : SyncOutcome;

    /// <summary>Silent token acquisition failed, either with a plain failure or one requiring interactive re-authentication.</summary>
    public sealed record AuthFailed(bool RequiresReAuth) : SyncOutcome;

    /// <summary>The sync pass itself required interactive re-authentication part-way through.</summary>
    public sealed record ReAuthRequired : SyncOutcome;

    /// <summary>The account has no folders selected to sync.</summary>
    public sealed record NoFoldersSelected : SyncOutcome;

    /// <summary>The sync pass ran and completed with one or more failed jobs.</summary>
    public sealed record CompletedWithErrors(int FailedJobCount) : SyncOutcome;

    /// <summary>The sync pass ran and completed with no failures.</summary>
    public sealed record Completed : SyncOutcome;

    /// <summary>The sync pass was cancelled.</summary>
    public sealed record Cancelled : SyncOutcome;

    /// <summary>The sync pass failed with an exception that does not have a dedicated outcome case.</summary>
    public sealed record UnexpectedError(Exception Cause) : SyncOutcome;
}
