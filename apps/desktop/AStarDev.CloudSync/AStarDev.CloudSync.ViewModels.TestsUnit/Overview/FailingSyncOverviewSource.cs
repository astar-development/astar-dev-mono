using AStarDev.CloudSync.Overview;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

internal sealed class FailingSyncOverviewSource(string message) : ISyncOverviewSource
{
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Failure<SyncOverview, ErrorResponse>(new ErrorResponse(message)));
}
