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
    /// <summary>The unique identifier of this conflict.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The remote item involved in the conflict.</summary>
    public RemoteItemRef Remote { get; init; } = RemoteItemRefFactory.Create(new AccountId(string.Empty), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty));

    /// <summary>The local file target involved in the conflict.</summary>
    public SyncFileTarget Target { get; init; } = SyncFileTargetFactory.Create(string.Empty, string.Empty);

    /// <summary>The local and remote state captured when the conflict was detected.</summary>
    public ConflictSnapshot Snapshot { get; init; } = ConflictSnapshotFactory.Create(DateTimeOffset.MinValue, 0L, DateTimeOffset.MinValue, 0L);

    /// <summary>The current resolution state of this conflict.</summary>
    public ConflictState State { get; set; } = ConflictState.Pending;

    /// <summary>The policy applied to resolve this conflict, when resolved.</summary>
    public Option<ConflictPolicy> Resolution { get; set; } = Option.None<ConflictPolicy>();

    /// <summary>The date and time the conflict was detected.</summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>The date and time the conflict was resolved, when resolved.</summary>
    public Option<DateTimeOffset> ResolvedAt { get; set; } = Option.None<DateTimeOffset>();
}
