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

    public async Task UpsertAsync(AccountId accountId, string remotePath, RuleType ruleType, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.SyncRules
            .FirstOrDefaultAsync(r => r.AccountId == accountId && r.RemotePath == remotePath, cancellationToken);

        if(existing is null)
        {
            _ = db.SyncRules.Add(new SyncRuleEntity { AccountId = accountId, RemotePath = remotePath, RuleType = ruleType });
        }
        else
        {
            existing.RuleType = ruleType;
        }

        _ = await db.SaveChangesAsync(cancellationToken);
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
}
