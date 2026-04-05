using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface ISyncRepository
{
    /// <summary>
    /// Enqueues new sync jobs to be processed by the SyncService.
    /// </summary>
    /// <param name="jobs">A list of sync jobs to enqueue.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnqueueJobsAsync(IEnumerable<SyncJob> jobs);

    /// <summary>
    /// Retrieves pending sync jobs for the specified account. This is called by the SyncService to get jobs that need to be processed.
    /// </summary>
    /// <param name="accountId">The ID of the account for which to retrieve pending jobs.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<List<SyncJobEntity>> GetPendingJobsAsync(string accountId);

    /// <summary>
    /// Updates the state of a sync job. This is called by the SyncService when a job's state changes (e.g., from pending to in-progress, or when it completes).
    /// </summary>
    /// <param name="jobId">The ID of the job to update.</param>
    /// <param name="state">The new state of the job.</param>
    /// <param name="stateError">An optional error message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateJobStateAsync(Guid jobId, SyncJobState state, string? stateError = null);

    /// <summary>
    /// Clears completed jobs for the specified account.
    /// </summary>
    /// <param name="accountId">The ID of the account for which to clear completed jobs.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearCompletedJobsAsync(string accountId);

    /// <summary>
    /// Adds a new sync conflict to the repository. This is called by the SyncService when it detects a conflict during synchronization.
    /// </summary>
    /// <param name="conflict">The conflict to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddConflictAsync(SyncConflict conflict);

    /// <summary>
    /// Retrieves pending conflicts for the specified account. This is called by the ActivityViewModel when the active account changes, to load conflicts into the UI.
    /// </summary>
    /// <param name="accountId">The ID of the account for which to retrieve pending conflicts.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<List<SyncConflictEntity>> GetPendingConflictsAsync(string accountId);

    /// <summary>
    /// Resolves a sync conflict with the specified resolution policy. This is called by the ActivityViewModel when the user resolves a conflict in the UI.
    /// </summary>
    /// <param name="conflictId">The ID of the conflict to resolve.</param>
    /// <param name="resolution">The resolution policy to apply.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResolveConflictAsync(Guid conflictId, ConflictPolicy resolution);

    /// <summary>
    /// Gets the count of pending conflicts for the specified account. This is called by the ActivityViewModel when the active account changes, to update the conflict badge in the UI.
    /// </summary>
    /// <param name="accountId">The ID of the account for which to get pending conflict count.</param>
    /// <returns></returns>
    Task<int> GetPendingConflictCountAsync(string accountId);
}
