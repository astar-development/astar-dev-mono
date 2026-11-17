using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.Sync;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync;

/// <summary>Starts the sync scheduler and, beside it, the classification migration and seed (#1854). The scheduler does not wait for the seed, and a seed that fails or is cancelled is logged and does not stop the scheduler.</summary>
/// <param name="classification">Migrates and seeds classification.</param>
/// <param name="scheduler">The sync scheduler to start.</param>
/// <param name="logger">Reports a seed or scheduler start that failed, by exception type only.</param>
public sealed class SyncStartup(IClassificationStartup classification, ISyncScheduler scheduler, ILogger<SyncStartup> logger)
{
    /// <summary>Starts classification in the background, then starts the scheduler unless <paramref name="cancellationToken"/> is already cancelled. Never throws.</summary>
    /// <param name="cancellationToken">Cancelled when the application shuts down; it cancels the seed and stops the scheduler from starting.</param>
    /// <returns>A task that completes when the scheduler has started and classification has finished.</returns>
    public Task RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{classification}{scheduler}{logger}{cancellationToken}");
}
