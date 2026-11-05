using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Core.Overview;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.Overview;

/// <summary>Builds the overview from the saved accounts. There is no sync engine yet, so sync pairs, activity, conflicts and throughput are empty, and every account shows no folders or files, up to date and never synced.</summary>
/// <param name="accountStore">Reads the saved accounts.</param>
public sealed class AccountStoreSyncOverviewSource(IAccountStore accountStore) : ISyncOverviewSource
{
    /// <inheritdoc />
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{accountStore}{cancellationToken}");
}
