using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

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
    public Option<ConflictPolicy> Resolution { get; set; } = Option.None<ConflictPolicy>();
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
    public Option<DateTimeOffset> ResolvedAt { get; set; } = Option.None<DateTimeOffset>();
}
