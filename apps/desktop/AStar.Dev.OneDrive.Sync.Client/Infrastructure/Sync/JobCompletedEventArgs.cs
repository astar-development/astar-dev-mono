using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public sealed class JobCompletedEventArgs(SyncJob job) : EventArgs
{
    public SyncJob Job { get; } = job;
}
