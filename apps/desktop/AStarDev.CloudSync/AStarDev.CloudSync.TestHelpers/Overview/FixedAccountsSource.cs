using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.Overview;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Overview;

public sealed class FixedAccountsSource(params CloudAccount[] accounts) : ISyncOverviewSource
{
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create(accounts, [], [], [], [], 0, Option.None<DateTimeOffset>())));
}
