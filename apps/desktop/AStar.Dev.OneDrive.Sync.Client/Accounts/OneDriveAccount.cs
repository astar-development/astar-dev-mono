using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

public sealed class OneDriveAccount
{
    /// <summary>Stable identifier — the Microsoft account object ID from MSAL.</summary>
    public AccountId Id { get; init; }

    /// <summary>Display name and email from the Microsoft profile.</summary>
    public AccountProfile Profile { get; set; } = AccountProfileFactory.Empty;

    /// <summary>
    /// Index into the fixed accent colour palette (0–5).
    /// Assigned sequentially when the account is added.
    /// </summary>
    public int AccentIndex { get; set; }

    /// <summary>
    /// Folder item IDs the user has chosen to sync.
    /// Empty means "not yet configured" (all excluded until set).
    /// </summary>
    public List<OneDriveFolderId> SelectedFolderIds { get; set; } = [];

    /// <summary>UTC timestamp of the last successful delta sync.</summary>
    public DateTimeOffset? LastSyncedAt { get; set; }

    /// <summary>OneDrive storage quota refreshed periodically from the Graph API.</summary>
    public StorageQuota Quota { get; set; } = StorageQuotaFactory.Unknown;

    /// <summary>Whether this account is currently active / selected in the UI.</summary>
    public bool IsActive { get; set; }

    /// <summary>Maps folder ID to display name — kept in sync with SelectedFolderIds.</summary>
    public Dictionary<OneDriveFolderId, string> FolderNames { get; set; } = [];

    /// <summary>Sync behaviour configuration. Null means not yet configured.</summary>
    public AccountSyncConfig? SyncConfig { get; set; }
}
