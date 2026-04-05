using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class SyncJobEntity
{
    public Guid Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string FolderId { get; set; } = string.Empty;
    public string RemoteItemId { get; set; } = string.Empty;
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
