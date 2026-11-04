using AStarDev.CloudSync.Core.Overview;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

internal sealed class FixedSyncOverviewSource(SyncOverview overview) : ISyncOverviewSource
{
    public Task<SyncOverview> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(overview);
}
