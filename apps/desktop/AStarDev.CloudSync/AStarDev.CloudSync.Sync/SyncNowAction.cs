using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="runner">Runs the sync.</param>
/// <param name="activity">Tells whether a run is in progress.</param>
/// <param name="cancel">Cancels the running run.</param>
/// <param name="dialog">Asks the user what to do when a run is in progress.</param>
internal sealed class SyncNowAction(ISyncPassRunner runner, ISyncActivitySource activity, ISyncCancelAction cancel, IConfirmSyncRestartDialog dialog) : ISyncNowAction
{
    /// <inheritdoc />
    public Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotImplementedException($"{runner}{activity}{cancel}{dialog}{accountId}{cancellationToken}");
}
