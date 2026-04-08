using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class AccountRepository(AppDbContext db) : IAccountRepository
{
    public Task<List<AccountEntity>> GetAllAsync()
        => db.Accounts
          .Include(a => a.SyncFolders)
          .OrderBy(a => a.Email)
          .ToListAsync();

    public Task<AccountEntity?> GetByIdAsync(string id)
        => db.Accounts
          .Include(a => a.SyncFolders)
          .FirstOrDefaultAsync(a => a.Id == id);

    public async Task UpsertAsync(AccountEntity account)
    {
        var existing = await db.Accounts
            .Include(a => a.SyncFolders)
            .FirstOrDefaultAsync(a => a.Id == account.Id);

        if(existing is null)
        {
            _ = db.Accounts.Add(account);
        }
        else
        {
            // Update scalar properties
            db.Entry(existing).CurrentValues.SetValues(account);

            // Sync folder collection — remove deleted, add new
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

        _ = await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id) => await db.Accounts.Where(a => a.Id == id).ExecuteDeleteAsync();

    public async Task SetActiveAccountAsync(string id)
    {
        // Clear all active flags then set the requested one
        _ = await db.Accounts.ExecuteUpdateAsync(s =>
            s.SetProperty(a => a.IsActive, false));

        _ = await db.Accounts
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(a => a.IsActive, true));
    }

    public async Task UpdateDeltaLinkAsync(string accountId, string folderId, string deltaLink) => await db.SyncFolders
            .Where(f => f.AccountId == accountId && f.FolderId == folderId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(f => f.DeltaLink, deltaLink));
}
