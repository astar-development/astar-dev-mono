using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class SyncRuleRepository(IDbContextFactory<AppDbContext> dbFactory) : ISyncRuleRepository
{
    public async Task<List<SyncRuleEntity>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.SyncRules
             .Where(r => r.AccountId == accountId)
             .ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(AccountId accountId, string remotePath, RuleType ruleType, string? remoteItemId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.Database.ExecuteSqlAsync(
            $"INSERT INTO SyncRules (AccountId, RemotePath, RuleType, RemoteItemId) VALUES ({accountId.Id}, {remotePath}, {(int)ruleType}, {remoteItemId}) ON CONFLICT(AccountId, RemotePath) DO UPDATE SET RuleType = excluded.RuleType, RemoteItemId = COALESCE(excluded.RemoteItemId, RemoteItemId)",
            cancellationToken);
    }

    public async Task DeleteAsync(AccountId accountId, string remotePath, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncRules
                   .Where(r => r.AccountId == accountId && r.RemotePath == remotePath)
                   .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncRules
                   .Where(r => r.AccountId == accountId)
                   .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteChildRulesAsync(AccountId accountId, string parentPath, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        string pattern = parentPath + "/%";
        _ = await db.Database.ExecuteSqlAsync(
            $"DELETE FROM SyncRules WHERE AccountId = {accountId.Id} AND RemotePath LIKE {pattern}",
            cancellationToken);
    }
}
