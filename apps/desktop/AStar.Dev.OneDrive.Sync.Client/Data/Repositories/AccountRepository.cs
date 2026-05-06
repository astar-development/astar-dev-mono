using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

/// <summary>
/// Repository for managing accounts in the database.
/// </summary>
/// <param name="dbFactory">The database context factory.</param>
public sealed class AccountRepository(IDbContextFactory<AppDbContext> dbFactory) : IAccountRepository
{
    /// <inheritdoc/>
    public async Task<List<AccountEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Accounts
          .OrderBy(a => a.Profile.Email)
          .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Option<AccountEntity>> GetByIdAsync(AccountId id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity is not null ? Option.Some(entity) : Option.None<AccountEntity>();
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(AccountEntity account, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken);

        if(existing is null)
        {
            _ = db.Accounts.Add(account);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(account);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(AccountId id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        _ = await db.Accounts.Where(a => a.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc/>
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
