using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

internal sealed class AccountRepository(AppDbContext dbContext) : IAccountRepository
{
    public async Task<bool> HasAnyAsync(CancellationToken ct = default)
        => await dbContext.Accounts.AnyAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default)
        => await dbContext.Accounts
            .AsNoTracking()
            .OrderBy(account => account.DisplayName)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<Account?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Id == id, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Account account, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        _ = dbContext.Accounts.Add(account);
        _ = await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        _ = dbContext.Accounts.Update(account);
        _ = await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveAsync(Guid id, CancellationToken ct = default)
    {
        var account = await dbContext.Accounts.FindAsync([id], ct).ConfigureAwait(false);

        if (account is null)
            return;

        _ = dbContext.Accounts.Remove(account);
        _ = await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(Guid AccountId, string LocalSyncPath)>> GetAllSyncPathsAsync(CancellationToken ct = default)
    {
        var rows = await dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.LocalSyncPath != string.Empty)
            .Select(account => new { account.Id, account.LocalSyncPath })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return [.. rows.Select(row => (row.Id, row.LocalSyncPath))];
    }
}
