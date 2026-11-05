using AStarDev.CloudSync.Core.Overview;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

internal sealed class CountingSyncOverviewSource(SyncOverview overview) : ISyncOverviewSource
{
    public int Reads { get; private set; }

    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken)
    {
        Reads++;

        return Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(overview));
    }
}
