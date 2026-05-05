namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>
/// Represents a single file operation queued by the sync engine.
/// Created from delta query results and processed in order.
/// Construct via <see cref="SyncJobFactory"/>.
/// </summary>
public sealed record SyncJob(RemoteItemRef Remote, SyncFileTarget Target, SyncFileMetadata Metadata, SyncDirection Direction, SyncJobStatus Status, string? DownloadUrl = null, string? UploadedRemoteItemId = null);
