using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class AccountRepository(AppDbContext db) : IAccountRepository
{
    public Task<List<AccountEntity>> GetAllAsync(CancellationToken cancellationToken)
        => db.Accounts
          .Include(a => a.SyncFolders)
          .OrderBy(a => a.Email)
          .ToListAsync(cancellationToken);

    public Task<AccountEntity?> GetByIdAsync(AccountId id, CancellationToken cancellationToken)
        => db.Accounts
          .Include(a => a.SyncFolders)
          .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task UpsertAsync(AccountEntity account, CancellationToken cancellationToken)
    {
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
        => await db.Accounts.Where(a => a.Id == id).ExecuteDeleteAsync(cancellationToken);

    public async Task SetActiveAccountAsync(AccountId id, CancellationToken cancellationToken)
    {
        _ = await db.Accounts.ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.IsActive, false), cancellationToken: cancellationToken);

        _ = await db.Accounts
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(a => a.IsActive, true), cancellationToken: cancellationToken);
    }

    public async Task UpdateDeltaLinkAsync(AccountId accountId, OneDriveFolderId folderId, string deltaLink, CancellationToken cancellationToken)
        => await db.SyncFolders
            .Where(f => f.AccountId == accountId && f.FolderId == folderId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(f => f.DeltaLink, deltaLink), cancellationToken);
}
