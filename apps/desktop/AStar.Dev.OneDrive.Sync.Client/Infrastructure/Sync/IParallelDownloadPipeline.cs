using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public interface IParallelDownloadPipeline
{
    Task RunAsync(IEnumerable<SyncJob> jobs, string accessToken, Action<SyncProgressEventArgs> onProgress, Action<JobCompletedEventArgs> onJobCompleted, string accountId, string folderId, int workerCount = 8, CancellationToken ct = default);
}
