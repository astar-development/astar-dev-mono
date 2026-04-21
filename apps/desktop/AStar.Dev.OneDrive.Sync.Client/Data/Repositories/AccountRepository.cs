using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class AccountRepository(IDbContextFactory<AppDbContext> dbFactory) : IAccountRepository
{
    public async Task<List<AccountEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Accounts
          .Include(a => a.SyncFolders)
          .OrderBy(a => a.Email)
          .ToListAsync(cancellationToken);
    }

    public async Task<AccountEntity?> GetByIdAsync(AccountId id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Accounts
          .Include(a => a.SyncFolders)
          .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task UpsertAsync(AccountEntity account, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Accounts
            .Include(a => a.SyncFolders)
            .FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken);

        if(existing is null)
        {
            _ = db.Accounts.Add(account);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(account);

            var toRemove = existing.SyncFolders
                .Where(f => account.SyncFolders.All(nf => nf.FolderId != f.FolderId))
                .ToList();

            db.SyncFolders.RemoveRange(toRemove);

            foreach(var newFolder in account.SyncFolders
                .Where(nf => existing.SyncFolders.All(f => f.FolderId != nf.FolderId)))
            {
                existing.SyncFolders.Add(newFolder);
            }
        }

        _ = await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(AccountId id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        _ = await db.Accounts.Where(a => a.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task SetActiveAccountAsync(AccountId id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.Accounts.ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.IsActive, false), cancellationToken: cancellationToken);

        _ = await db.Accounts
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(a => a.IsActive, true), cancellationToken: cancellationToken);
    }
}
