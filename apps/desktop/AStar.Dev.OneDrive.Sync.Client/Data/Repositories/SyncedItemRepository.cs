using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

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

    public async Task<int> UpsertAsync(SyncedItemEntity item, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.SyncedItems
            .FirstOrDefaultAsync(i => i.AccountId == item.AccountId && i.RemoteItemId == item.RemoteItemId, cancellationToken);

        if(existing is null)
        {
            _ = db.SyncedItems.Add(item);
            _ = await db.SaveChangesAsync(cancellationToken);

            return item.Id;
        }

        existing.RemoteParentId   = item.RemoteParentId;
        existing.RemotePath       = item.RemotePath;
        existing.LocalPath        = item.LocalPath;
        existing.IsFolder         = item.IsFolder;
        existing.RemoteModifiedAt = item.RemoteModifiedAt;
        existing.Tags             = item.Tags;
        _ = await db.SaveChangesAsync(cancellationToken);

        return existing.Id;
    }

    public async Task UpsertClassificationsAsync(int syncedItemId, IReadOnlyList<FileClassification> classifications, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        _ = await db.SyncedItemClassifications
                   .Where(c => c.SyncedItemId == syncedItemId)
                   .ExecuteDeleteAsync(cancellationToken);

        var entities = classifications.Select(c => new SyncedItemClassificationEntity
        {
            SyncedItemId = syncedItemId,
            Level1       = c.Level1,
            Level2       = c.Level2.MapOrDefault(v => v, null),
            Level3       = c.Level3.MapOrDefault(v => v, null),
            TagName      = c.TagName,
            IsSpecial    = c.IsSpecial
        });

        db.SyncedItemClassifications.AddRange(entities);
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
