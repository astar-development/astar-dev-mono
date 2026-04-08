namespace AStar.Dev.OneDrive.Sync.Client.Models;

/// <summary>
/// Per-account user-configurable settings.
/// Stored as a JSON column on AccountEntity in a later migration,
/// or as separate columns — kept as a plain model for now.
/// </summary>
public sealed class AccountSettings
{
    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.Ignore;

    /// <summary>
    /// Root local path for this account's synced files.
    /// e.g. /home/user/OneDrive/personal@outlook.com
    /// </summary>
    public string LocalSyncPath { get; set; } = string.Empty;
}
