using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.TestsUnit;

internal sealed class StartupFakeScheduler : ISyncScheduler
{
    internal bool Throws { get; init; }

    internal int Starts { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Starts++;

        return Throws ? Task.FromException(new InvalidOperationException("user@example.com token-123")) : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
