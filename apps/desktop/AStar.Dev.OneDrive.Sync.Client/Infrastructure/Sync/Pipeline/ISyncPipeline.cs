using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>Orchestrates a parallel sync pipeline over a collection of <see cref="SyncJob"/> items.</summary>
public interface ISyncPipeline
{
    /// <summary>Runs all <paramref name="jobs"/> concurrently using up to <paramref name="workerCount"/> workers, reporting progress and completion via the supplied callbacks.</summary>
    /// <param name="jobs">The jobs to process.</param>
    /// <param name="tokenFactory">The token factory used to obtain an access token for each worker.</param>
    /// <param name="onProgress">Invoked after each job completes with aggregate progress information.</param>
    /// <param name="onJobCompleted">Invoked after each job completes with the resulting <see cref="SyncJob"/>.</param>
    /// <param name="accountId">The account identifier used in progress events.</param>
    /// <param name="folderId">The folder identifier used in progress events.</param>
    /// <param name="workerCount">Maximum number of concurrent workers. Defaults to 8.</param>
    /// <param name="ct">Token used to cancel the pipeline.</param>
    Task RunAsync(IEnumerable<SyncJob> jobs, Func<CancellationToken, Task<string>> tokenFactory, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, string accountId, string folderId, int workerCount = 8, CancellationToken ct = default);
}
