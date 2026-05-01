using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class SyncRuleRepository(IDbContextFactory<AppDbContext> dbFactory) : ISyncRuleRepository
{
    private const string UpsertSql =
        "INSERT INTO SyncRules (AccountId, RemotePath, RuleType, RemoteItemId) " +
        "VALUES (@accountId, @remotePath, @ruleType, @remoteItemId) " +
        "ON CONFLICT(AccountId, RemotePath) DO UPDATE SET RuleType = excluded.RuleType, " +
        "RemoteItemId = COALESCE(excluded.RemoteItemId, RemoteItemId)";

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

        List<object> parameters =
        [
            new SqliteParameter("@accountId",    accountId.Id),
            new SqliteParameter("@remotePath",   remotePath),
            new SqliteParameter("@ruleType",     (int)ruleType),
            new SqliteParameter("@remoteItemId", (object?)remoteItemId ?? DBNull.Value),
        ];

        _ = await db.Database.ExecuteSqlRawAsync(UpsertSql, parameters, cancellationToken);
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

        string prefix = parentPath + "/";
        _ = await db.SyncRules
                   .Where(r => r.AccountId == accountId && r.RemotePath.StartsWith(prefix))
                   .ExecuteDeleteAsync(cancellationToken);
    }
}
