using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="syncLock">The per-account lock every trigger goes through.</param>
/// <param name="steps">The four steps of a run.</param>
/// <param name="registry">Told when a run starts and ends.</param>
/// <param name="states">Set to sign-in required when a step reports it.</param>
/// <param name="timeProvider">Times the steps.</param>
/// <param name="logger">Logs the trigger, step timings and counts, with the hashed account id only.</param>
internal sealed class SyncPassRunner(IAccountSyncLock syncLock, SyncPassSteps steps, SyncPassRegistry registry, IAccountSignInStates states, TimeProvider timeProvider, ILogger<SyncPassRunner> logger) : ISyncPassRunner
{
    /// <inheritdoc />
    public Task<Result<SyncPassSummary, SyncPassFailure>> RunAsync(AccountId accountId, SyncTrigger trigger, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{syncLock}{steps}{registry}{states}{timeProvider}{logger}{accountId}{trigger}{cancellationToken}");
}
