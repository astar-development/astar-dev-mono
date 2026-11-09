using AStarDev.CloudSync.Overview;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

internal sealed class SequencedSyncOverviewSource(IReadOnlyList<Task<Result<SyncOverview, ErrorResponse>>> reads) : ISyncOverviewSource
{
    private int next;

    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => reads[Interlocked.Increment(ref next) - 1];
}
