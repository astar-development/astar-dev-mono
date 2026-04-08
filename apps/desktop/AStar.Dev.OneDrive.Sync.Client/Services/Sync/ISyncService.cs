using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

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
    Task SyncAccountAsync(OneDriveAccount account, CancellationToken ct = default);
    /// <summary>
    /// Applies a conflict resolution to a pending conflict and
    /// re-queues the appropriate file operation.
    /// </summary>
    Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken ct = default);

    /// <summary>Raised whenever sync progress changes for any account.</summary>
    event EventHandler<SyncProgressEventArgs> SyncProgressChanged;

    /// <summary>Raised when a file job completes (success or failure).</summary>
    event EventHandler<JobCompletedEventArgs> JobCompleted;

    /// <summary>Raised when a new conflict is detected and queued.</summary>
    event EventHandler<SyncConflict> ConflictDetected;
}
