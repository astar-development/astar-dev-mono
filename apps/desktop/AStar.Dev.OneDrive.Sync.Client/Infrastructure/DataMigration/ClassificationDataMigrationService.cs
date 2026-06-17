using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.DataMigration;

/// <inheritdoc />
public sealed class ClassificationDataMigrationService(IDbContextFactory<AppDbContext> dbFactory, ICategoryResolutionService categoryResolutionService, ILogger<ClassificationDataMigrationService> logger) : IClassificationDataMigrationService
{
    private sealed record OldClassificationRow(int SyncedItemId, string Level1, string? Level2, string? Level3, bool IsSpecial);

    private const int BatchSize = 1_000;
    private const string OldTableName = "SyncedItemClassifications";

    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        if (await db.SyncedItemFileClassifications.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

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
            {
                var classifications = group.Select(row => FileClassificationFactory.Create(
                    row.Level1,
                    string.IsNullOrEmpty(row.Level2) ? Option.None<string>() : Option.Some(row.Level2),
                    string.IsNullOrEmpty(row.Level3) ? Option.None<string>() : Option.Some(row.Level3),
                    row.IsSpecial)).ToList();

                var categoryIds = await categoryResolutionService.ResolveManyAsync(classifications, cancellationToken).ConfigureAwait(false);

                db.SyncedItemFileClassifications.AddRange(categoryIds.Select(categoryId => new SyncedItemFileClassificationEntity
                {
                    SyncedItemId = group.Key,
                    CategoryId = categoryId
                }));
            }

            _ = await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            totalMigrated += batch.Count;
            offset += BatchSize;
        }

        OneDriveSyncClientMessages.ClassificationDataMigrated(logger, totalMigrated);
    }

    private static async Task<bool> OldTableExistsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var count = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sqlite_master WHERE type='table' AND name={OldTableName}")
            .FirstAsync(cancellationToken).ConfigureAwait(false);

        return count > 0;
    }

    private static async Task<IReadOnlyList<OldClassificationRow>> ReadOldBatchAsync(AppDbContext db, int offset, CancellationToken cancellationToken)
        => await db.Database
            .SqlQuery<OldClassificationRow>($"SELECT SyncedItemId, Level1, Level2, Level3, IsSpecial FROM SyncedItemClassifications LIMIT {BatchSize} OFFSET {offset}")
            .ToListAsync(cancellationToken).ConfigureAwait(false);
}
