using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>Orchestrates a parallel sync pipeline over a stream of <see cref="SyncJob"/> items.</summary>
public interface ISyncPipeline
{
    /// <summary>
    /// Runs all <paramref name="jobs"/> concurrently using up to <paramref name="workerCount"/> workers,
    /// reporting progress and completion via the supplied callbacks.
    /// Returns the number of jobs that failed.
    /// </summary>
    /// <param name="jobs">The async stream of jobs to process.</param>
    /// <param name="tokenFactory">The token factory used to obtain an access token for each worker.</param>
    /// <param name="onProgress">Invoked after each job completes with aggregate progress information.</param>
    /// <param name="onJobCompleted">Invoked after each job completes with the resulting <see cref="SyncJob"/>.</param>
    /// <param name="accountId">The account identifier used in progress events.</param>
    /// <param name="folderId">The folder identifier used in progress events.</param>
    /// <param name="workerCount">Maximum number of concurrent workers. Defaults to 4.</param>
    /// <param name="ct">Token used to cancel the pipeline.</param>
    /// <returns>The count of jobs that did not complete successfully.</returns>
    Task<int> RunAsync(IAsyncEnumerable<SyncJob> jobs, Func<CancellationToken, Task<string>> tokenFactory, Action<SyncProgressEventArgs> onProgress, Func<JobCompletedEventArgs, Task> onJobCompleted, string accountId, string folderId, int workerCount = 4, CancellationToken ct = default);
}
