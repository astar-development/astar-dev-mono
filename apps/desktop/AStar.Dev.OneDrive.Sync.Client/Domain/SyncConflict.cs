namespace AStar.Dev.OneDrive.Sync.Client.Domain;

public enum ConflictState { Pending, Resolved, Skipped }

/// <summary>
/// Represents a file conflict detected during a delta sync pass.
/// Queued for user resolution or automatic policy application.
/// </summary>
public sealed class SyncConflict
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public RemoteItemRef Remote { get; init; } = RemoteItemRefFactory.Create(new AccountId(string.Empty), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty));
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
