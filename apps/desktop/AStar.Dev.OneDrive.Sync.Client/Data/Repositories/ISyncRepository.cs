using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;


namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface ISyncRepository
{
    /// <summary>Enqueues new sync jobs to be processed by the SyncService.</summary>
    /// <param name="jobs">A list of sync jobs to enqueue.</param>
    Task EnqueueJobsAsync(IEnumerable<SyncJob> jobs);

    /// <summary>Retrieves pending sync jobs for the specified account.</summary>
    /// <param name="accountId">The ID of the account for which to retrieve pending jobs.</param>
    Task<List<SyncJobEntity>> GetPendingJobsAsync(AccountId accountId);

    /// <summary>Updates the state of a sync job.</summary>
    /// <param name="jobId">The ID of the job to update.</param>
    /// <param name="state">The new state of the job.</param>
    /// <param name="stateError">An optional error message.</param>
    Task UpdateJobStateAsync(Guid jobId, SyncJobState state, Option<string> stateError);

    /// <summary>Clears completed jobs for the specified account.</summary>
    /// <param name="accountId">The ID of the account for which to clear completed jobs.</param>
    Task ClearCompletedJobsAsync(AccountId accountId);

    /// <summary>Adds a new sync conflict to the repository.</summary>
    /// <param name="conflict">The conflict to add.</param>
    Task AddConflictAsync(SyncConflict conflict);

    /// <summary>Retrieves pending conflicts for the specified account.</summary>
    /// <param name="accountId">The ID of the account for which to retrieve pending conflicts.</param>
    Task<List<SyncConflictEntity>> GetPendingConflictsAsync(AccountId accountId);

    /// <summary>Resolves a sync conflict with the specified resolution policy.</summary>
    /// <param name="conflictId">The ID of the conflict to resolve.</param>
    /// <param name="resolution">The resolution policy to apply.</param>
    Task ResolveConflictAsync(Guid conflictId, ConflictPolicy resolution);

    /// <summary>Gets the count of pending conflicts for the specified account.</summary>
    /// <param name="accountId">The ID of the account for which to get pending conflict count.</param>
    Task<int> GetPendingConflictCountAsync(AccountId accountId);
}
