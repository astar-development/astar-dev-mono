namespace AStar.Dev.OneDrive.Sync.Client.Models;

/// <summary>
/// Represents a single file operation queued by the sync engine.
/// Created from delta query results and processed in order.
/// </summary>
public sealed record SyncJob(string AccountId, string FolderId, string RemoteItemId, string RelativePath, string LocalPath, SyncDirection Direction, long FileSize, DateTimeOffset RemoteModified,
    Guid Id, DateTimeOffset QueuedAt, SyncJobState State = SyncJobState.Queued, string? ErrorMessage = null, string? DownloadUrl = null, DateTimeOffset? CompletedAt = null,
    string? UploadedRemoteItemId = null);
