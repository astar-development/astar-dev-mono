using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class SyncWorkerFactory(IEnumerable<IJobHandler> handlers, ISyncRepository syncRepository) : ISyncWorkerFactory
{
    private readonly IReadOnlyList<IJobHandler> _handlers = handlers.ToList().AsReadOnly();

    /// <inheritdoc />
    public ISyncWorker Create(int workerId) => new SyncWorker(workerId, _handlers, syncRepository);
}
