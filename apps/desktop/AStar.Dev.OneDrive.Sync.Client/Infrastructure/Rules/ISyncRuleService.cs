using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;

/// <summary>
/// Encapsulates sync-rule persistence logic so view models never access <see cref="Data.Repositories.ISyncRuleRepository" /> directly.
/// </summary>
public interface ISyncRuleService
{
    /// <summary>Removes all rules beneath <paramref name="parentRemotePath" />, upserts the supplied nodes with <paramref name="ruleType" />, and returns the resulting count of include rules for the account.</summary>
    Task<int> ApplyRuleAsync(AccountId accountId, string parentRemotePath, RuleType ruleType, IReadOnlyList<(string RemotePath, string Id)> nodes, CancellationToken cancellationToken);

    /// <summary>Returns a dictionary mapping each persisted rule's remote path to its <see cref="RuleType" />, compared case-insensitively.</summary>
    Task<IReadOnlyDictionary<string, RuleType>> GetRuleStatesAsync(AccountId accountId, CancellationToken cancellationToken);
}
