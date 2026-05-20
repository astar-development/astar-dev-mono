namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>Creates <see cref="ISyncWorker"/> instances for the parallel sync pipeline.</summary>
public interface ISyncWorkerFactory
{
    /// <summary>Creates a new worker with the given <paramref name="workerId"/>.</summary>
    ISyncWorker Create(int workerId);
}
