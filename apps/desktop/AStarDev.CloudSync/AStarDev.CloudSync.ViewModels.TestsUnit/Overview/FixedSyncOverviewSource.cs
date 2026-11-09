using AStarDev.CloudSync.Overview;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

internal sealed class FixedSyncOverviewSource(SyncOverview overview) : ISyncOverviewSource
{
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(overview));
}
