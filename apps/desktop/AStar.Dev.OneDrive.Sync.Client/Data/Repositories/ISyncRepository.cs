using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;


namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface ISyncRepository
{
    /// <summary>Enqueues new sync jobs to be processed by the SyncService.</summary>
    /// <param name="jobs">A list of sync jobs to enqueue.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task EnqueueJobsAsync(IEnumerable<SyncJob> jobs, CancellationToken cancellationToken = default);

    /// <summary>Enqueues a single sync job with <see cref="SyncJobState.Queued"/> state.</summary>
    Task EnqueueJobAsync(SyncJob job, CancellationToken cancellationToken = default);

    /// <summary>Retrieves pending sync jobs for the specified account.</summary>
    /// <param name="accountId">The ID of the account for which to retrieve pending jobs.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task<List<SyncJobEntity>> GetPendingJobsAsync(AccountId accountId, CancellationToken cancellationToken = default);

    /// <summary>Updates the state of a sync job.</summary>
    /// <param name="jobId">The ID of the job to update.</param>
    /// <param name="state">The new state of the job.</param>
    /// <param name="stateError">An optional error message.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task UpdateJobStateAsync(Guid jobId, SyncJobState state, Option<string> stateError, CancellationToken cancellationToken = default);

    /// <summary>Clears completed jobs for the specified account.</summary>
    /// <param name="accountId">The ID of the account for which to clear completed jobs.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task ClearCompletedJobsAsync(AccountId accountId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new sync conflict to the repository.</summary>
    /// <param name="conflict">The conflict to add.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task AddConflictAsync(SyncConflict conflict, CancellationToken cancellationToken = default);

    /// <summary>Retrieves pending conflicts for the specified account.</summary>
    /// <param name="accountId">The ID of the account for which to retrieve pending conflicts.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task<List<SyncConflictEntity>> GetPendingConflictsAsync(AccountId accountId, CancellationToken cancellationToken = default);

    /// <summary>Resolves a sync conflict with the specified resolution policy.</summary>
    /// <param name="conflictId">The ID of the conflict to resolve.</param>
    /// <param name="resolution">The resolution policy to apply.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task ResolveConflictAsync(Guid conflictId, ConflictPolicy resolution, CancellationToken cancellationToken = default);

    /// <summary>Gets the count of pending conflicts for the specified account.</summary>
    /// <param name="accountId">The ID of the account for which to get pending conflict count.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    Task<int> GetPendingConflictCountAsync(AccountId accountId, CancellationToken cancellationToken = default);
}
