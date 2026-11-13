namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="DownloadItemOutcome"/> values.</summary>
public static class DownloadItemOutcomeFactory
{
    /// <summary>Creates the <see cref="DownloadItemOutcome.Downloaded"/> case. A negative size reads as zero.</summary>
    public static DownloadItemOutcome CreateDownloaded(long bytes) => new DownloadItemOutcome.Downloaded(Math.Max(bytes, 0));

    /// <summary>Creates the <see cref="DownloadItemOutcome.FolderCreated"/> case.</summary>
    public static DownloadItemOutcome CreateFolderCreated() => new DownloadItemOutcome.FolderCreated();

    /// <summary>Creates the <see cref="DownloadItemOutcome.Moved"/> case.</summary>
    public static DownloadItemOutcome CreateMoved() => new DownloadItemOutcome.Moved();

    /// <summary>Creates the <see cref="DownloadItemOutcome.DeletedLocally"/> case.</summary>
    public static DownloadItemOutcome CreateDeletedLocally() => new DownloadItemOutcome.DeletedLocally();

    /// <summary>Creates the <see cref="DownloadItemOutcome.HeldByGuard"/> case.</summary>
    public static DownloadItemOutcome CreateHeldByGuard() => new DownloadItemOutcome.HeldByGuard();

    /// <summary>Creates the <see cref="DownloadItemOutcome.SentToConflicts"/> case.</summary>
    public static DownloadItemOutcome CreateSentToConflicts(ConflictKind kind) => new DownloadItemOutcome.SentToConflicts(kind);

    /// <summary>Creates the <see cref="DownloadItemOutcome.Skipped"/> case.</summary>
    public static DownloadItemOutcome CreateSkipped(DownloadSkipReason reason) => new DownloadItemOutcome.Skipped(reason);

    /// <summary>Creates the <see cref="DownloadItemOutcome.Failed"/> case.</summary>
    public static DownloadItemOutcome CreateFailed(DownloadFailure failure) => new DownloadItemOutcome.Failed(failure);
}
