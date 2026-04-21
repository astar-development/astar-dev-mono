using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

public sealed class SyncedItemRepository(IDbContextFactory<AppDbContext> dbFactory) : ISyncedItemRepository
{
    public async Task<Dictionary<string, SyncedItemEntity>> GetAllByAccountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var items = await db.SyncedItems
            .Where(i => i.AccountId == accountId)
            .ToListAsync(cancellationToken);

        return items.ToDictionary(i => i.RemoteItemId.Id);
    }

    public async Task UpsertAsync(SyncedItemEntity item, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.SyncedItems
            .FirstOrDefaultAsync(i => i.AccountId == item.AccountId && i.RemoteItemId == item.RemoteItemId, cancellationToken);

        if(existing is null)
        {
            _ = db.SyncedItems.Add(item);
        }
        else
        {
            existing.RemoteParentId   = item.RemoteParentId;
            existing.RemotePath       = item.RemotePath;
            existing.LocalPath        = item.LocalPath;
            existing.IsFolder         = item.IsFolder;
            existing.RemoteModifiedAt = item.RemoteModifiedAt;
            existing.ETag             = item.ETag;
        }

        _ = await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByRemoteIdAsync(AccountId accountId, OneDriveItemId remoteItemId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncedItems
                   .Where(i => i.AccountId == accountId && i.RemoteItemId == remoteItemId)
                   .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncedItems
                   .Where(i => i.AccountId == accountId)
                   .ExecuteDeleteAsync(cancellationToken);
    }
}
