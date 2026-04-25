using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class SyncedItemEntity
{
    public int Id { get; set; }
    public AccountId AccountId { get; set; }
    public OneDriveItemId RemoteItemId { get; set; }
    public string RemoteParentId { get; set; } = string.Empty;
    public string RemotePath { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public DateTimeOffset RemoteModifiedAt { get; set; }
    public string? ETag { get; set; }
    public string? CTag { get; set; }

    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
