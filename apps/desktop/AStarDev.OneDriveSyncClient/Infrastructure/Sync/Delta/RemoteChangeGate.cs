using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <inheritdoc />
public sealed class RemoteChangeGate(IGraphService graphService, IDriveStateRepository driveStateRepository, TimeProvider timeProvider, IOptions<SyncSettings> syncSettings, ILogger<RemoteChangeGate> logger) : IRemoteChangeGate
{
    private const string WalkOutcome = "Walk";
    private const string SkipOutcome = "Skip";

    /// <inheritdoc />
    public async Task<RemoteWalkDecision> DecideAsync(OneDriveAccount account, DriveId driveId, Func<CancellationToken, Task<string>> tokenFactory, IReadOnlyList<SyncRuleEntity> rules, IReadOnlyDictionary<string, SyncedItemEntity> syncedItems, CancellationToken cancellationToken)
    {
        var driveState = await driveStateRepository.GetByAccountIdAsync(account.Id, cancellationToken).ConfigureAwait(false);
        var storedState = driveState.Match(state => state, () => new DriveStateEntity { AccountId = account.Id });
        var maxAge = TimeSpan.FromHours(syncSettings.Value.FullEnumerationMaxAgeHours);
        var plan = RemoteEnumerationPlanner.Plan(storedState.DeltaLink, storedState.RulesFingerprint, RulesFingerprintCalculator.Compute(rules), storedState.LastFullEnumerationAt, timeProvider.GetUtcNow(), maxAge, rules);

        var decision = plan switch
        {
            CheckForChanges check => await CheckForRelevantChangesAsync(account.Id.Value, driveId, tokenFactory, check.DeltaLink, rules, syncedItems, cancellationToken).ConfigureAwait(false),
            WalkRequired walk => await WalkWithLatestLinkAsync(account.Id.Value, driveId, tokenFactory, walk.Reason, cancellationToken).ConfigureAwait(false),
            _ => RemoteWalkDecisionFactory.CreateWalk(WalkReasons.DeltaUnavailable, Option.None<string>())
        };

        LogDecision(account.Id.Value, decision);

        return decision;
    }

    private async Task<RemoteWalkDecision> CheckForRelevantChangesAsync(string accountId, DriveId driveId, Func<CancellationToken, Task<string>> tokenFactory, string deltaLink, IReadOnlyList<SyncRuleEntity> rules, IReadOnlyDictionary<string, SyncedItemEntity> syncedItems, CancellationToken cancellationToken)
    {
        var result = await graphService.GetDeltaChangesAsync(tokenFactory, driveId, deltaLink, cancellationToken).ConfigureAwait(false);

        if (result is not Ok<DeltaQueryResult, string> { Value: DeltaChangesFound found })
        {
            string error = result is Fail<DeltaQueryResult, string> failure ? failure.Error : "Delta link expired or unusable";
            OneDriveSyncClientMessages.RemoteChangeGateDeltaFailed(logger, accountId, error);

            return await WalkWithLatestLinkAsync(accountId, driveId, tokenFactory, WalkReasons.DeltaUnavailable, cancellationToken).ConfigureAwait(false);
        }

        return RemoteChangeRelevance.AnyAffectSyncedScope(found.Changes, syncedItems, rules)
            ? RemoteWalkDecisionFactory.CreateWalk(WalkReasons.RelevantChanges, Option.Some(found.NextDeltaLink))
            : RemoteWalkDecisionFactory.CreateSkip(found.NextDeltaLink);
    }

    private async Task<RemoteWalkDecision> WalkWithLatestLinkAsync(string accountId, DriveId driveId, Func<CancellationToken, Task<string>> tokenFactory, string reason, CancellationToken cancellationToken)
    {
        var latest = await graphService.GetLatestDeltaLinkAsync(tokenFactory, driveId, cancellationToken).ConfigureAwait(false);

        if (latest is Fail<string, string> failure)
            OneDriveSyncClientMessages.RemoteChangeGateDeltaFailed(logger, accountId, failure.Error);

        return RemoteWalkDecisionFactory.CreateWalk(reason, latest is Ok<string, string> ok ? Option.Some(ok.Value) : Option.None<string>());
    }

    private void LogDecision(string accountId, RemoteWalkDecision decision)
    {
        string outcome = decision is SkipRemote ? SkipOutcome : WalkOutcome;
        string reason = decision is WalkRemote walk ? walk.Reason : WalkReasons.NoChanges;
        OneDriveSyncClientMessages.RemoteChangeGateDecided(logger, outcome, accountId, reason);
    }
}
