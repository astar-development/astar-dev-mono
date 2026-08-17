using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

/// <inheritdoc />
public sealed class SyncWorkerFactory(IEnumerable<IJobHandler> handlers, ISyncRepository syncRepository, ILogger<SyncWorker> workerLogger) : ISyncWorkerFactory
{
    private readonly IReadOnlyList<IJobHandler> handlers = handlers.ToList().AsReadOnly();

    /// <inheritdoc />
    public ISyncWorker Create(int workerId) => new SyncWorker(workerId, handlers, syncRepository, workerLogger);
}
