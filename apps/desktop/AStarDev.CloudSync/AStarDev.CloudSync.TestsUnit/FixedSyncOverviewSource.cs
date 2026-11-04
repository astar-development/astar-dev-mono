using AStarDev.CloudSync.Core;

namespace AStarDev.CloudSync.TestsUnit;

internal sealed class FixedSyncOverviewSource(SyncOverview overview) : ISyncOverviewSource
{
    public Task<SyncOverview> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(overview);
}
