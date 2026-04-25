using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public interface ISyncRuleRepository
{
    /// <summary>Returns all sync rules for the specified account.</summary>
    Task<List<SyncRuleEntity>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Inserts or replaces the rule for the given account and remote path. When <paramref name="remoteItemId"/> is null and a row already exists, the existing RemoteItemId is preserved.</summary>
    Task UpsertAsync(AccountId accountId, string remotePath, RuleType ruleType, string? remoteItemId, CancellationToken cancellationToken);

    /// <summary>Removes all rules for the specified account and remote path.</summary>
    Task DeleteAsync(AccountId accountId, string remotePath, CancellationToken cancellationToken);

    /// <summary>Removes all rules for the specified account.</summary>
    Task DeleteAllAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Removes all rules whose remote path starts with <paramref name="parentPath"/>/ for the specified account.</summary>
    Task DeleteChildRulesAsync(AccountId accountId, string parentPath, CancellationToken cancellationToken);
}
