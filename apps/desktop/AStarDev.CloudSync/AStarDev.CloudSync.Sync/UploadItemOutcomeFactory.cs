namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="UploadItemOutcome"/> cases.</summary>
public static class UploadItemOutcomeFactory
{
    /// <summary>Creates the <see cref="UploadItemOutcome.Uploaded"/> case.</summary>
    public static UploadItemOutcome CreateUploaded(RelativeSyncPath path, long bytes) => new UploadItemOutcome.Uploaded(path, bytes);

    /// <summary>Creates the <see cref="UploadItemOutcome.FolderCreated"/> case.</summary>
    public static UploadItemOutcome CreateFolderCreated(RelativeSyncPath path) => new UploadItemOutcome.FolderCreated(path);

    /// <summary>Creates the <see cref="UploadItemOutcome.Linked"/> case.</summary>
    public static UploadItemOutcome CreateLinked(RelativeSyncPath path) => new UploadItemOutcome.Linked(path);

    /// <summary>Creates the <see cref="UploadItemOutcome.RemovedRemotely"/> case.</summary>
    public static UploadItemOutcome CreateRemovedRemotely(RelativeSyncPath path) => new UploadItemOutcome.RemovedRemotely(path);

    /// <summary>Creates the <see cref="UploadItemOutcome.SentToConflicts"/> case.</summary>
    public static UploadItemOutcome CreateSentToConflicts(RelativeSyncPath path, ConflictKind kind) => new UploadItemOutcome.SentToConflicts(path, kind);

    /// <summary>Creates the <see cref="UploadItemOutcome.Deferred"/> case.</summary>
    public static UploadItemOutcome CreateDeferred(RelativeSyncPath path) => new UploadItemOutcome.Deferred(path);

    /// <summary>Creates the <see cref="UploadItemOutcome.Rejected"/> case.</summary>
    public static UploadItemOutcome CreateRejected(RelativeSyncPath path, InvalidRemoteName reason) => new UploadItemOutcome.Rejected(path, reason);

    /// <summary>Creates the <see cref="UploadItemOutcome.Failed"/> case.</summary>
    public static UploadItemOutcome CreateFailed(RelativeSyncPath path, ProviderError error) => new UploadItemOutcome.Failed(path, error);
}
