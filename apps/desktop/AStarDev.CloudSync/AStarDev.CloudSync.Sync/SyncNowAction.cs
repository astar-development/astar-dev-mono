using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="runner">Runs the sync.</param>
/// <param name="activity">Tells whether a run is in progress.</param>
/// <param name="cancel">Cancels the running sync.</param>
/// <param name="prompt">Asks the SE-08 question.</param>
/// <param name="outcomes">Stores "could not start" when the run was refused although nothing was running.</param>
internal sealed class SyncNowAction(ISyncPassRunner runner, ISyncActivitySource activity, ISyncCancelAction cancel, ISyncRestartPrompt prompt, ISyncOutcomeWriter outcomes) : ISyncNowAction
{
    /// <inheritdoc />
    public Task SyncNowAsync(AccountId accountId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{runner}{activity}{cancel}{prompt}{outcomes}{accountId}{cancellationToken}");
}
