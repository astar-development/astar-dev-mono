using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <summary>Handles a specific type of <see cref="SyncJob"/>.</summary>
public interface IJobHandler
{
    /// <summary>Returns true when this handler is capable of processing <paramref name="job"/>.</summary>
    bool CanHandle(SyncJob job);

    /// <summary>Executes <paramref name="job"/> and returns the completed job or an error message.</summary>
    Task<Result<SyncJob, string>> HandleAsync(SyncJob job, string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken ct);
}
