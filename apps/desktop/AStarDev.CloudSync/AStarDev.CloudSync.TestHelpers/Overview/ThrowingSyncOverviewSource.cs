using AStarDev.CloudSync.Core.Overview;

namespace AStarDev.CloudSync.TestHelpers.Overview;

public sealed class ThrowingSyncOverviewSource(Exception exception) : ISyncOverviewSource
{
    public Task<SyncOverview> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromException<SyncOverview>(exception);
}
