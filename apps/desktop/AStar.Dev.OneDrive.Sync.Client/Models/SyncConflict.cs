namespace AStar.Dev.OneDrive.Sync.Client.Models;

public enum ConflictState { Pending, Resolved, Skipped }

/// <summary>
/// Represents a file conflict detected during a delta sync pass.
/// Queued for user resolution or automatic policy application.
/// </summary>
public sealed class SyncConflict
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string AccountId { get; init; } = string.Empty;
    public string FolderId { get; init; } = string.Empty;
    public string RemoteItemId { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public string LocalPath { get; init; } = string.Empty;

    public DateTimeOffset LocalModified { get; init; }
    public DateTimeOffset RemoteModified { get; init; }
    public long LocalSize { get; init; }
    public long RemoteSize { get; init; }

    public ConflictState State { get; set; } = ConflictState.Pending;
    public ConflictPolicy? Resolution { get; set; }
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}
