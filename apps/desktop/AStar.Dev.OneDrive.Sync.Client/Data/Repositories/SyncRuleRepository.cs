using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class SyncRuleRepository(IDbContextFactory<AppDbContext> dbFactory) : ISyncRuleRepository
{
    public async Task<List<SyncRuleEntity>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.SyncRules
             .Where(r => r.AccountId == accountId)
             .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertAsync(AccountId accountId, string remotePath, RuleType ruleType, string? remoteItemId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.SyncRules
            .FirstOrDefaultAsync(r => r.AccountId == accountId && r.RemotePath == remotePath, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            db.SyncRules.Add(new SyncRuleEntity
            {
                AccountId = accountId,
                RemotePath = remotePath,
                RuleType = ruleType,
                RemoteItemId = remoteItemId is not null ? Option.Some(remoteItemId) : Option.None<string>()
            });
        }
        else
        {
            existing.RuleType = ruleType;

            if (remoteItemId is not null)
                existing.RemoteItemId = Option.Some(remoteItemId);
        }

        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(AccountId accountId, string remotePath, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncRules
                   .Where(r => r.AccountId == accountId && r.RemotePath == remotePath)
                   .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAllAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncRules
                   .Where(r => r.AccountId == accountId)
                   .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteChildRulesAsync(AccountId accountId, string parentPath, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        string prefix = parentPath + "/";
        _ = await db.SyncRules
                   .Where(r => r.AccountId == accountId && r.RemotePath.StartsWith(prefix))
                   .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
