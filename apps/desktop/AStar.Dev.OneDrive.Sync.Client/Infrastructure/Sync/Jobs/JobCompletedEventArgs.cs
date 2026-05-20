using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

public sealed class JobCompletedEventArgs(SyncJob job) : EventArgs
{
    public SyncJob Job { get; } = job;
}
