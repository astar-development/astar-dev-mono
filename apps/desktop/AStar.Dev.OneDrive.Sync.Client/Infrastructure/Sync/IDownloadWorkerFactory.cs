namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>Creates <see cref="IDownloadWorker"/> instances for the parallel download pipeline.</summary>
public interface IDownloadWorkerFactory
{
    /// <summary>Creates a new worker with the given <paramref name="workerId"/>.</summary>
    IDownloadWorker Create(int workerId);
}
