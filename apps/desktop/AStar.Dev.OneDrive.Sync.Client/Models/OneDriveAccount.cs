namespace AStar.Dev.OneDrive.Sync.Client.Models;

public sealed class OneDriveAccount
{
    /// <summary>Stable identifier — the Microsoft account object ID from MSAL.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
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
    /// Folder item IDs the user has chosen to sync.
    /// Empty means "not yet configured" (all excluded until set).
    /// </summary>
    public List<string> SelectedFolderIds { get; set; } = [];
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
    /// <summary>Maps folder ID to display name — kept in sync with SelectedFolderIds.</summary>
    public Dictionary<string, string> FolderNames { get; set; } = [];
    // Sync settings
    public string LocalSyncPath { get; set; } = string.Empty;
    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.Ignore;
}
