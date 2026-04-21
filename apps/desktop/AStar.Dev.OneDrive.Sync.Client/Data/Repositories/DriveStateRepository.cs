using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class DriveStateRepository(IDbContextFactory<AppDbContext> dbFactory) : IDriveStateRepository
{
    public async Task<DriveStateEntity?> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.DriveStates.FirstOrDefaultAsync(d => d.AccountId == accountId, cancellationToken);
    }

    public async Task UpsertAsync(DriveStateEntity driveState, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.DriveStates
            .FirstOrDefaultAsync(d => d.AccountId == driveState.AccountId, cancellationToken);

        if(existing is null)
        {
            _ = db.DriveStates.Add(driveState);
        }
        else
        {
            existing.DeltaLink         = driveState.DeltaLink;
            existing.LastSyncStartedAt = driveState.LastSyncStartedAt;
        }

        _ = await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearDeltaLinkAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.DriveStates
            .Where(d => d.AccountId == accountId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.DeltaLink, (string?)null), cancellationToken);
    }
}
