namespace AStarDev.CloudSync.Sync;

/// <summary>Why an interrupted sync could not be resumed at startup, and what the user has to fix first (#1612). The resume itself needs no action: the next sync run after the cause is fixed finishes the work.</summary>
public abstract record RecoveryBlocker
{
    private RecoveryBlocker()
    {
    }

    /// <summary>The account must sign in again.</summary>
    public sealed record SignInRequired : RecoveryBlocker
    {
        internal SignInRequired()
        {
        }
    }

    /// <summary>The account's sync folder is missing, unmounted or not set.</summary>
    public sealed record SyncFolderMissing : RecoveryBlocker
    {
        internal SyncFolderMissing()
        {
        }
    }

    /// <summary>The CloudSync database could not be read, or failed its integrity check, so nothing is synced.</summary>
    public sealed record DatabaseUnreadable : RecoveryBlocker
    {
        internal DatabaseUnreadable()
        {
        }
    }
}
