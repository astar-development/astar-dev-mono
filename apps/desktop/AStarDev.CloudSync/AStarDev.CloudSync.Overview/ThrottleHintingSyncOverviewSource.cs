using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Overview;

/// <summary>Adds the throttle hint (#1463) to each account of the overview, leaving everything else as it was.</summary>
/// <param name="inner">Builds the overview.</param>
/// <param name="hints">Tells which accounts the provider keeps throttling.</param>
internal sealed class ThrottleHintingSyncOverviewSource(AccountStoreSyncOverviewSource inner, IAccountThrottleHints hints) : ISyncOverviewSource
{
    /// <inheritdoc />
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) =>
        inner.GetOverviewAsync(cancellationToken).MatchAsync(overview => Result.Success<SyncOverview, ErrorResponse>(Hinted(overview)), Result.Failure<SyncOverview, ErrorResponse>);

    private SyncOverview Hinted(SyncOverview overview) => SyncOverviewFactory.Create([.. overview.Accounts.Select(account => account.WithThrottleHint(hints.HintFor(account.Id)))], overview.SyncPairs, overview.Activity, overview.Conflicts, overview.Throughput, overview.BytesMovedToday, overview.LastChecked);
}
