using AStar.Dev.Functional.Extensions;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.DataMigration;

/// <inheritdoc />
public sealed class ClassificationDataMigrationService(IDbContextFactory<AppDbContext> dbFactory, ICategoryResolutionService categoryResolutionService, IFileDetailResolver fileDetailResolver, IFileClassificationRepository fileClassificationRepository, ILogger<ClassificationDataMigrationService> logger) : IClassificationDataMigrationService
{
    private sealed record OldClassificationRow(int SyncedItemId, string Level1, string? Level2, string? Level3, bool IsFamous, bool IsInternet);

    private const int BatchSize = 1_000;
    private const string OldTableName = "SyncedItemClassifications";

    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        if (!await OldTableExistsAsync(db, cancellationToken).ConfigureAwait(false))
            return;

        int totalMigrated = 0;
        int offset = 0;

        while (true)
        {
            var batch = await ReadOldBatchAsync(db, offset, cancellationToken).ConfigureAwait(false);

            if (batch.Count == 0)
                break;

            foreach (var group in batch.GroupBy(row => row.SyncedItemId))
                totalMigrated += await MigrateItemAsync(db, group.Key, [.. group], cancellationToken).ConfigureAwait(false);

            offset += BatchSize;
        }

        _ = await db.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {OldTableName}", cancellationToken).ConfigureAwait(false);

        OneDriveSyncClientMessages.ClassificationDataMigrated(logger, totalMigrated);
    }

    private async Task<int> MigrateItemAsync(AppDbContext db, int syncedItemId, IReadOnlyList<OldClassificationRow> rows, CancellationToken cancellationToken)
    {
        var syncedItem = await db.SyncedItems.FirstOrDefaultAsync(i => i.Id == syncedItemId, cancellationToken).ConfigureAwait(false);

        if (syncedItem is null || string.IsNullOrEmpty(syncedItem.LocalPath))
            return 0;

        var fileDetail = await fileDetailResolver.FindOrCreateAsync(syncedItem.LocalPath, syncedItem.SizeInBytes, cancellationToken).ConfigureAwait(false);

        if (syncedItem.FileDetailId != fileDetail.Id)
        {
            syncedItem.FileDetailId = fileDetail.Id;
            _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (await fileClassificationRepository.HasClassificationsAsync(fileDetail.Id, cancellationToken).ConfigureAwait(false))
            return 0;

        var classifications = rows.Select(row => FileClassificationFactory.Create(
            row.Level1,
            string.IsNullOrEmpty(row.Level2) ? Option.None<string>() : Option.Some(row.Level2),
            string.IsNullOrEmpty(row.Level3) ? Option.None<string>() : Option.Some(row.Level3),
            row.IsFamous, row.IsInternet)).ToList();

        var categoryIds = await categoryResolutionService.ResolveManyAsync(classifications, cancellationToken).ConfigureAwait(false);
        await fileClassificationRepository.AddClassificationsAsync(fileDetail.Id, categoryIds, cancellationToken).ConfigureAwait(false);

        return rows.Count;
    }

    private static async Task<bool> OldTableExistsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        int count = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sqlite_master WHERE type='table' AND name={OldTableName}")
            .FirstAsync(cancellationToken).ConfigureAwait(false);

        return count > 0;
    }

    private static async Task<IReadOnlyList<OldClassificationRow>> ReadOldBatchAsync(AppDbContext db, int offset, CancellationToken cancellationToken)
        => await db.Database
            .SqlQuery<OldClassificationRow>($"SELECT SyncedItemId, Level1, Level2, Level3, IsSpecial FROM SyncedItemClassifications LIMIT {BatchSize} OFFSET {offset}")
            .ToListAsync(cancellationToken).ConfigureAwait(false);
}
