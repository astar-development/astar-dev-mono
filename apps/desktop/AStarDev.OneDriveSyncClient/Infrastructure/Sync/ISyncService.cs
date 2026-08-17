using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync;

/// <summary>
/// Orchestrates bidirectional delta sync for a single account.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Runs a full delta sync for all selected folders on the given account.
    /// Progress is reported via <see cref="SyncProgressChanged"/>.
    /// Conflicts are queued — not blocked on.
    /// </summary>
    Task SyncAccountAsync(OneDriveAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a conflict resolution to a pending conflict and
    /// re-queues the appropriate file operation.
    /// </summary>
    Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>Raised when sync progress updates are available for an account. This includes overall sync progress as well as granular progress for individual file jobs.</summary>
    event EventHandler<SyncProgressEventArgs> SyncProgressChanged;

    /// <summary>Raised when an individual file sync job completes, providing details about the completed job and its result. This allows subscribers to react to specific file operations completing, such as updating the UI or triggering follow-up actions.</summary>
    event EventHandler<JobCompletedEventArgs> JobCompleted;

    /// <summary>Raised when a sync conflict is detected during the sync process. Subscribers can handle this event to present conflict resolution options to the user or to automatically apply a resolution policy. The event args will include details about the conflict, such as the account, file, and nature of the conflict.</summary>
    event EventHandler<SyncConflict> ConflictDetected;

    /// <summary>Raised when a pending conflict has been successfully resolved and persisted.</summary>
    event EventHandler<SyncConflict> ConflictResolved;
}
