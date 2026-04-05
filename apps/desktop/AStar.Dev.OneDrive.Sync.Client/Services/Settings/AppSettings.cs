using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Settings;

/// <summary>
/// Application-level settings persisted to JSON alongside the DB.
/// Account-specific settings (LocalSyncPath, ConflictPolicy) live on AccountEntity.
/// </summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public string Locale { get; set; } = "en-GB";
    public ConflictPolicy DefaultConflictPolicy { get; set; } = ConflictPolicy.Ignore;
    public int SyncIntervalMinutes { get; set; } = 60;
}
