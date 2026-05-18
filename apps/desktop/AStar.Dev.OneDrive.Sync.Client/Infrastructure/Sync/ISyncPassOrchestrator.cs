using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Orchestrates a single sync pass for one account: enumerate remote, detect deletions, detect local changes, and execute jobs.
/// </summary>
public interface ISyncPassOrchestrator
{
    /// <summary>
    /// Runs the full sync pass pipeline for <paramref name="account"/> using <paramref name="token"/> for Graph API calls.
    /// Returns <see langword="true"/> when at least one sync rule was active; <see langword="false"/> when no folders were selected.
    /// </summary>
    Task<bool> OrchestrateAsync(OneDriveAccount account, string token, Func<SyncConflict, Task> conflictCallback, Action<SyncProgressEventArgs>? onProgress = null, Action<JobCompletedEventArgs>? onJobCompleted = null, CancellationToken ct = default);
}
