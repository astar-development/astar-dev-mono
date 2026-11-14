namespace AStarDev.CloudSync.Sync;

/// <summary>Which part of a sync run an account is in (#1563).</summary>
public abstract record SyncPhase
{
    private SyncPhase()
    {
    }

    /// <summary>Bringing the remote index up to date.</summary>
    public sealed record UpdatingRemoteIndex : SyncPhase
    {
        internal UpdatingRemoteIndex()
        {
        }
    }

    /// <summary>Scanning the local side for changes.</summary>
    public sealed record ScanningLocal : SyncPhase
    {
        internal ScanningLocal()
        {
        }
    }

    /// <summary>Copying remote changes to the local side.</summary>
    public sealed record Downloading : SyncPhase
    {
        internal Downloading()
        {
        }
    }

    /// <summary>Copying local changes to the remote side.</summary>
    public sealed record Uploading : SyncPhase
    {
        internal Uploading()
        {
        }
    }

    /// <summary>Recording the paths both sides changed.</summary>
    public sealed record RecordingConflicts : SyncPhase
    {
        internal RecordingConflicts()
        {
        }
    }
}
