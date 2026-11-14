namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncNotification"/> cases.</summary>
public static class SyncNotificationFactory
{
    /// <summary>Creates the <see cref="SyncNotification.Synced"/> case.</summary>
    /// <param name="files">How many files changed.</param>
    public static SyncNotification CreateSynced(int files) => new SyncNotification.Synced(Math.Max(files, 0));

    /// <summary>Creates the <see cref="SyncNotification.ConflictsFound"/> case.</summary>
    /// <param name="count">How many conflicts.</param>
    public static SyncNotification CreateConflictsFound(int count) => new SyncNotification.ConflictsFound(Math.Max(count, 0));

    /// <summary>Creates the <see cref="SyncNotification.DeletionsHeld"/> case.</summary>
    /// <param name="count">How many deletions are held.</param>
    public static SyncNotification CreateDeletionsHeld(int count) => new SyncNotification.DeletionsHeld(Math.Max(count, 0));

    /// <summary>Creates the <see cref="SyncNotification.SignInRequired"/> case.</summary>
    public static SyncNotification CreateSignInRequired() => new SyncNotification.SignInRequired();

    /// <summary>Creates the <see cref="SyncNotification.SyncFailed"/> case.</summary>
    public static SyncNotification CreateSyncFailed() => new SyncNotification.SyncFailed();
}
