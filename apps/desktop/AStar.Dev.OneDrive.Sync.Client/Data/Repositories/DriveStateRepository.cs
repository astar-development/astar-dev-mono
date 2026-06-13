using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class DriveStateRepository(IDbContextFactory<AppDbContext> dbFactory) : IDriveStateRepository
{
    public async Task<Option<DriveStateEntity>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.DriveStates.FirstOrDefaultAsync(d => d.AccountId == accountId, cancellationToken).ConfigureAwait(false);

        return entity is null ? Option.None<DriveStateEntity>() : new Option<DriveStateEntity>.Some(entity);
    }

    public async Task UpsertAsync(DriveStateEntity driveState, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.DriveStates
            .FirstOrDefaultAsync(d => d.AccountId == driveState.AccountId, cancellationToken).ConfigureAwait(false);

        if(existing is null)
        {
            _ = db.DriveStates.Add(driveState);
        }
        else
        {
            existing.DeltaLink         = driveState.DeltaLink;
            existing.LastSyncStartedAt = driveState.LastSyncStartedAt;
        }

        _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearDeltaLinkAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.DriveStates
            .Where(d => d.AccountId == accountId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.DeltaLink, Option.None<string>()), cancellationToken).ConfigureAwait(false);
    }
}
