using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Models;

public sealed class OneDriveAccount
{
    /// <summary>Stable identifier — the Microsoft account object ID from MSAL.</summary>
    public AccountId Id { get; init; }
    /// <summary>Display name from the Microsoft profile (e.g. "Jason Smith").</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Email / UPN (e.g. jason@outlook.com).</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>
    /// Index into the fixed accent colour palette (0–5).
    /// Assigned sequentially when the account is added.
    /// </summary>
    public int AccentIndex { get; set; }
    /// <summary>
    /// Root-level folder IDs the user has chosen to sync.
    /// Used by the sync engine to drive delta API calls.
    /// Empty means "not yet configured" (all excluded until set).
    /// </summary>
    public List<OneDriveFolderId> SelectedFolderIds { get; set; } = [];
    /// <summary>
    /// All included folder IDs at any depth — root and nested.
    /// Used by the UI to restore folder inclusion state on restart.
    /// </summary>
    public List<OneDriveFolderId> AllIncludedFolderIds { get; set; } = [];
    /// <summary>
    /// Folder IDs that are explicitly excluded even though their parent folder is included.
    /// Persisted so exclusions survive app restarts.
    /// </summary>
    public List<OneDriveFolderId> ExplicitlyExcludedFolderIds { get; set; } = [];
    /// <summary>
    /// Delta link token from the last successful Graph delta query.
    /// Null means a full sync is required.
    /// </summary>
    public string? DeltaLink { get; set; }
    /// <summary>UTC timestamp of the last successful delta sync.</summary>
    public DateTimeOffset? LastSyncedAt { get; set; }
    /// <summary>Total OneDrive quota in bytes (refreshed periodically).</summary>
    public long QuotaTotal { get; set; }
    /// <summary>Used OneDrive quota in bytes.</summary>
    public long QuotaUsed { get; set; }
    /// <summary>Whether this account is currently active / selected in the UI.</summary>
    public bool IsActive { get; set; }
    /// <summary>Maps every known root folder ID to its display name. Superset of SelectedFolderIds — includes excluded folders so names are available for the exclusion UI.</summary>
    public Dictionary<OneDriveFolderId, string> FolderNames { get; set; } = [];
    /// <summary>Validated local path where files are synced. Null means not yet configured.</summary>
    public LocalSyncPath? LocalSyncPath { get; set; }
    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.Ignore;
}
