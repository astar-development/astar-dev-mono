using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <inheritdoc />
public sealed class SyncWorkerFactory(IEnumerable<IJobHandler> handlers, ISyncRepository syncRepository, ILogger<SyncWorker> workerLogger) : ISyncWorkerFactory
{
    private readonly IReadOnlyList<IJobHandler> handlers = handlers.ToList().AsReadOnly();

    /// <inheritdoc />
    public ISyncWorker Create(int workerId) => new SyncWorker(workerId, handlers, syncRepository, workerLogger);
}
