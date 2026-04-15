using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class SyncJobEntity
{
    public Guid Id { get; set; }
    public AccountId AccountId { get; set; }
    public OneDriveFolderId FolderId { get; set; }
    public OneDriveItemId RemoteItemId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public SyncDirection Direction { get; set; }
    public SyncJobState State { get; set; } = SyncJobState.Queued;
    public string? ErrorMessage { get; set; }
    public string? DownloadUrl { get; set; }
    public long FileSize { get; set; }
    public DateTimeOffset RemoteModified { get; set; }
    public DateTimeOffset QueuedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public AccountEntity? Account { get; set; }
}
