namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Creates <see cref="SyncJob"/> instances.</summary>
public static class SyncJobFactory
{
    /// <summary>Creates a new <see cref="SyncJob"/> from pre-constructed domain objects.</summary>
    public static SyncJob Create(RemoteItemRef remote, SyncFileTarget target, SyncFileMetadata metadata, SyncDirection direction, SyncJobStatus status, string? downloadUrl = null, string? uploadedRemoteItemId = null)
        => new(remote, target, metadata, direction, status, downloadUrl, uploadedRemoteItemId);
}
