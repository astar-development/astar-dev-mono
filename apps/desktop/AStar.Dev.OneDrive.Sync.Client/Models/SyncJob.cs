namespace AStar.Dev.OneDrive.Sync.Client.Models;

/// <summary>
/// Represents a single file operation queued by the sync engine.
/// Created from delta query results and processed in order.
/// </summary>
public sealed record SyncJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string AccountId { get; init; } = string.Empty;
    public string FolderId { get; init; } = string.Empty;
    public string RemoteItemId { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string LocalPath { get; init; } = string.Empty;
    public SyncDirection Direction { get; init; }
    public SyncJobState State { get; set; } = SyncJobState.Queued;
    public string? ErrorMessage { get; set; }
    public string? DownloadUrl { get; set; }
    public long FileSize { get; init; }
    public DateTimeOffset RemoteModified { get; init; }
    public DateTimeOffset QueuedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? UploadedRemoteItemId { get; set; }
}
