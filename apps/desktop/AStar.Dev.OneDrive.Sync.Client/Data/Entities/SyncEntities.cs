using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class SyncConflictEntity
{
    public Guid Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string FolderId { get; set; } = string.Empty;
    public string RemoteItemId { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public DateTimeOffset LocalModified { get; set; }
    public DateTimeOffset RemoteModified { get; set; }
    public long LocalSize { get; set; }
    public long RemoteSize { get; set; }
    public ConflictState State { get; set; } = ConflictState.Pending;
    public ConflictPolicy? Resolution { get; set; }
    public DateTimeOffset DetectedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
