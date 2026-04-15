using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class AccountEntity
{
    public AccountId Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AccentIndex { get; set; }
    public bool IsActive { get; set; }
    public string? DeltaLink { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public long QuotaTotal { get; set; }
    public long QuotaUsed { get; set; }
    public LocalSyncPath LocalSyncPath { get; set; } = LocalSyncPath.Restore(string.Empty);
    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.Ignore;
    public List<SyncFolderEntity> SyncFolders { get; set; } = [];
}
