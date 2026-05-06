using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>State-transition extensions for <see cref="SyncJob"/>.</summary>
public static class SyncJobExtensions
{
    /// <summary>Returns a copy of <paramref name="job"/> transitioned to <see cref="SyncJobState.Completed"/>.</summary>
    public static SyncJob Complete(this SyncJob job) => job with { Status = job.Status with { State = SyncJobState.Completed, CompletedAt = DateTimeOffset.UtcNow } };

    /// <summary>Returns a copy of <paramref name="job"/> transitioned to <see cref="SyncJobState.Failed"/>.</summary>
    public static SyncJob Fail(this SyncJob job, string? errorMessage = null) => job with { Status = job.Status with { State = SyncJobState.Failed, ErrorMessage = errorMessage, CompletedAt = DateTimeOffset.UtcNow } };
}
