using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class AccountEntity
{
    public AccountId Id { get; set; } = new AccountId("Unknown");
    public AccountProfile Profile { get; set; } = AccountProfileFactory.Empty;
    public int AccentIndex { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public long QuotaTotal { get; set; }
    public long QuotaUsed { get; set; }
    public AccountSyncConfig SyncConfig { get; set; } = AccountSyncConfigFactory.Default;
}
