namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>
/// Base type for all sync file operations queued by the sync engine.
/// Construct via <see cref="SyncJobFactory"/> — never use derived constructors directly.
/// </summary>
public abstract record SyncJob(RemoteItemRef Remote, SyncFileTarget Target, SyncFileMetadata Metadata, SyncJobStatus Status);

/// <summary>Download a remote file to the local path.</summary>
public sealed record DownloadSyncJob(RemoteItemRef Remote, SyncFileTarget Target, SyncFileMetadata Metadata, SyncJobStatus Status, string? DownloadUrl = null)
    : SyncJob(Remote, Target, Metadata, Status);

/// <summary>Upload a local file to OneDrive.</summary>
public sealed record UploadSyncJob(RemoteItemRef Remote, SyncFileTarget Target, SyncFileMetadata Metadata, SyncJobStatus Status, string? UploadedRemoteItemId = null)
    : SyncJob(Remote, Target, Metadata, Status);

/// <summary>Delete a local file that no longer exists on OneDrive.</summary>
public sealed record DeleteSyncJob(RemoteItemRef Remote, SyncFileTarget Target, SyncFileMetadata Metadata, SyncJobStatus Status)
    : SyncJob(Remote, Target, Metadata, Status);
