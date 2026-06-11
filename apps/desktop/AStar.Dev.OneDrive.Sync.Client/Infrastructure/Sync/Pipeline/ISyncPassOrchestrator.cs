using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>
/// Orchestrates a single sync pass for one account: enumerate remote, detect deletions, detect local changes, and execute jobs.
/// </summary>
public interface ISyncPassOrchestrator
{
    /// <summary>
    /// Runs the full sync pass pipeline for <paramref name="account"/> using <paramref name="tokenFactory"/> for Graph API calls.
    /// <paramref name="syncConfig"/> is the unwrapped sync configuration, resolved by the caller before invoking this method.
    /// Returns <see langword="true"/> when at least one sync rule was active; <see langword="false"/> when no folders were selected.
    /// </summary>
    Task<bool> OrchestrateAsync(OneDriveAccount account, AccountSyncConfig syncConfig, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncConflict, Task> conflictCallback, Action<SyncProgressEventArgs>? onProgress = null, Action<JobCompletedEventArgs>? onJobCompleted = null, CancellationToken ct = default);
}
