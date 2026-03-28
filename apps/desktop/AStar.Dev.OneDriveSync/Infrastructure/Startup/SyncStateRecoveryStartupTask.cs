namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed class SyncStateRecoveryStartupTask : IStartupTask
{
    public const string TaskName = "SyncStateRecovery";

    string IStartupTask.Name => TaskName;

    // Stub — sync state recovery will be implemented by the sync-engine feature story
    public Task RunAsync(CancellationToken ct) => Task.CompletedTask;
}
