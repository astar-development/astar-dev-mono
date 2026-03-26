namespace AStar.Dev.OneDriveSync.old.Models;

/// <summary>AM-01 → AM-07: Persisted account configuration.</summary>
public sealed record AccountRecord
{
    public string AccountId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string LocalSyncPath { get; init; } = string.Empty;
    public int SyncIntervalMinutes { get; init; } = 60;
    public int Concurrency { get; init; } = 4;
    public bool IsDebugLoggingEnabled { get; init; }
    public List<SelectedFolder> SelectedFolders { get; init; } = [];
}

public sealed record SelectedFolder
{
    public string FolderId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
