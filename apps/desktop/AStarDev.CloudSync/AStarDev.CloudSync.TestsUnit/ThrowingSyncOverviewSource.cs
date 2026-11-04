using AStarDev.CloudSync.Core;

namespace AStarDev.CloudSync.TestsUnit;

internal sealed class ThrowingSyncOverviewSource(Exception exception) : ISyncOverviewSource
{
    public Task<SyncOverview> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromException<SyncOverview>(exception);
}
