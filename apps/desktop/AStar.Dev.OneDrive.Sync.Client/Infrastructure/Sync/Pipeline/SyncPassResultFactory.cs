namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>Creates <see cref="SyncPassResult"/> instances.</summary>
public static class SyncPassResultFactory
{
    /// <inheritdoc cref="SyncPassResult"/>
    public static SyncPassResult Create(bool didRun, int failedJobCount) => new(didRun, failedJobCount);
}
