using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Rules;

/// <summary>
/// Default <see cref="ISyncRuleService" /> implementation backed by <see cref="ISyncRuleRepository" />.
/// </summary>
public sealed class SyncRuleService(ISyncRuleRepository syncRuleRepository, ILogger<SyncRuleService> logger) : ISyncRuleService
{
    /// <inheritdoc />
    public async Task<int> ApplyRuleAsync(AccountId accountId, string parentRemotePath, RuleType ruleType, IReadOnlyList<(string RemotePath, string Id)> nodes, CancellationToken cancellationToken)
    {
        string ruleTypeName = ruleType.ToString();
        OneDriveSyncClientMessages.RulePersisting(logger, ruleTypeName, parentRemotePath, accountId.Value);

        await syncRuleRepository.DeleteChildRulesAsync(accountId, parentRemotePath, cancellationToken).ConfigureAwait(false);

        foreach (var (remotePath, remoteItemId) in nodes)
            await syncRuleRepository.UpsertAsync(accountId, remotePath, ruleType, remoteItemId, cancellationToken).ConfigureAwait(false);

        var rules = await syncRuleRepository.GetByAccountIdAsync(accountId, cancellationToken).ConfigureAwait(false);

        return rules.Count(rule => rule.RuleType == RuleType.Include);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, RuleType>> GetRuleStatesAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var rules = await syncRuleRepository.GetByAccountIdAsync(accountId, cancellationToken).ConfigureAwait(false);

        return rules.ToDictionary(rule => rule.RemotePath, rule => rule.RuleType, StringComparer.OrdinalIgnoreCase);
    }
}
