namespace AStarDev.CloudSync.Sync;

/// <summary>What a finished pass is worth telling the user about while the window is not in front (#1410). It holds counts only, never a name or a path.</summary>
public abstract record SyncNotification
{
    private SyncNotification()
    {
    }

    /// <summary>The pass changed files on either side.</summary>
    public sealed record Synced : SyncNotification
    {
        internal Synced(int files) => Files = files;

        /// <summary>How many files were downloaded, uploaded or deleted.</summary>
        public int Files { get; }
    }

    /// <summary>The pass found conflicts.</summary>
    public sealed record ConflictsFound : SyncNotification
    {
        internal ConflictsFound(int count) => Count = count;

        /// <summary>How many conflicts.</summary>
        public int Count { get; }
    }

    /// <summary>The mass-deletion guard held deletions for the user to confirm.</summary>
    public sealed record DeletionsHeld : SyncNotification
    {
        internal DeletionsHeld(int count) => Count = count;

        /// <summary>How many deletions are held.</summary>
        public int Count { get; }
    }

    /// <summary>The account has to sign in again.</summary>
    public sealed record SignInRequired : SyncNotification
    {
        internal SignInRequired()
        {
        }
    }

    /// <summary>The pass failed for a reason the user may want to look at.</summary>
    public sealed record SyncFailed : SyncNotification
    {
        internal SyncFailed()
        {
        }
    }
}
