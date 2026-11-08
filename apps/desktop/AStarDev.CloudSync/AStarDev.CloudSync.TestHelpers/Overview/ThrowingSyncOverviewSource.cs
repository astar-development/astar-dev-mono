using AStarDev.CloudSync.Overview;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Overview;

public sealed class ThrowingSyncOverviewSource(Exception exception) : ISyncOverviewSource
{
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromException<Result<SyncOverview, ErrorResponse>>(exception);
}
