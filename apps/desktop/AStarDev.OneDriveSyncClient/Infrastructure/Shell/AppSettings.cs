using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Infrastructure.Theme;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

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
    public int ConcurrentWorkerCount { get; set; } = 4;
}
