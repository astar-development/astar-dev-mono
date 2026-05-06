using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>
/// Represents a file conflict detected during a delta sync pass.
/// Queued for user resolution or automatic policy application.
/// </summary>
public sealed class SyncConflict
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public RemoteItemRef Remote { get; init; } = RemoteItemRefFactory.Create(new AccountId(string.Empty), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty));
    public SyncFileTarget Target { get; init; } = SyncFileTargetFactory.Create(string.Empty, string.Empty);
    public ConflictSnapshot Snapshot { get; init; } = ConflictSnapshotFactory.Create(DateTimeOffset.MinValue, 0L, DateTimeOffset.MinValue, 0L);

    public ConflictState State { get; set; } = ConflictState.Pending;
    public ConflictPolicy? Resolution { get; set; }
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}
