using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;

/// <summary>
/// Default <see cref="ISyncRuleService" /> implementation backed by <see cref="ISyncRuleRepository" />.
/// </summary>
public sealed class SyncRuleService(ISyncRuleRepository syncRuleRepository, ILogger<SyncRuleService> logger) : ISyncRuleService
{
    /// <inheritdoc />
    public async Task<int> ApplyRuleAsync(AccountId accountId, string parentRemotePath, RuleType ruleType, IReadOnlyList<(string RemotePath, string Id)> nodes, CancellationToken cancellationToken)
    {
        string ruleTypeName = ruleType.ToString();
        OneDriveSyncClientMessages.RulePersisting(logger, ruleTypeName, parentRemotePath, accountId.Id);

        await syncRuleRepository.DeleteChildRulesAsync(accountId, parentRemotePath, cancellationToken);

        foreach (var (remotePath, remoteItemId) in nodes)
            await syncRuleRepository.UpsertAsync(accountId, remotePath, ruleType, remoteItemId, cancellationToken);

        var rules = await syncRuleRepository.GetByAccountIdAsync(accountId, cancellationToken);

        return rules.Count(rule => rule.RuleType == RuleType.Include);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, RuleType>> GetRuleStatesAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var rules = await syncRuleRepository.GetByAccountIdAsync(accountId, cancellationToken);

        return rules.ToDictionary(rule => rule.RemotePath, rule => rule.RuleType, StringComparer.OrdinalIgnoreCase);
    }
}
