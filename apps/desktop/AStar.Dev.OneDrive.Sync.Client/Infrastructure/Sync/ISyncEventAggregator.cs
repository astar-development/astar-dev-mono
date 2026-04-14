using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Aggregates sync events from <see cref="ISyncService"/> and <see cref="ISyncScheduler"/> and
/// re-raises them on the UI thread so child view models can subscribe without taking direct
/// dependencies on those services.
/// </summary>
public interface ISyncEventAggregator
{
    /// <summary>Raised whenever sync progress changes for any account.</summary>
    event EventHandler<SyncProgressEventArgs> SyncProgressChanged;

    /// <summary>Raised when a file job completes (success or failure).</summary>
    event EventHandler<JobCompletedEventArgs> JobCompleted;

    /// <summary>Raised when a new conflict is detected and queued.</summary>
    event EventHandler<SyncConflict> ConflictDetected;

    /// <summary>Raised when a full sync pass for an account has completed.</summary>
    event EventHandler<string> SyncCompleted;
}
