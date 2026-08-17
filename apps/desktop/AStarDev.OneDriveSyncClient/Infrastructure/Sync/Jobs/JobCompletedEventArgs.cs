using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;

public sealed class JobCompletedEventArgs(SyncJob job) : EventArgs
{
    public SyncJob Job { get; } = job;
}
