namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncPhase"/> values.</summary>
public static class SyncPhaseFactory
{
    /// <summary>Creates the <see cref="SyncPhase.UpdatingRemoteIndex"/> case.</summary>
    public static SyncPhase CreateUpdatingRemoteIndex() => new SyncPhase.UpdatingRemoteIndex();

    /// <summary>Creates the <see cref="SyncPhase.ScanningLocal"/> case.</summary>
    public static SyncPhase CreateScanningLocal() => new SyncPhase.ScanningLocal();

    /// <summary>Creates the <see cref="SyncPhase.Downloading"/> case.</summary>
    public static SyncPhase CreateDownloading() => new SyncPhase.Downloading();

    /// <summary>Creates the <see cref="SyncPhase.Uploading"/> case.</summary>
    public static SyncPhase CreateUploading() => new SyncPhase.Uploading();

    /// <summary>Creates the <see cref="SyncPhase.RecordingConflicts"/> case.</summary>
    public static SyncPhase CreateRecordingConflicts() => new SyncPhase.RecordingConflicts();
}
