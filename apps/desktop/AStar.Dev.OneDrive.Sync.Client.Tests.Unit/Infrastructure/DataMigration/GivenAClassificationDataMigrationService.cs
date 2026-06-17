using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.DataMigration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.DataMigration;

public sealed class GivenAClassificationDataMigrationService
{
    private static (AppDbContext seedingContext, IDbContextFactory<AppDbContext> factory, SqliteConnection connection) CreateSqliteFactory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS SyncedItemClassifications (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SyncedItemId INTEGER NOT NULL,
                    Level1 TEXT NOT NULL,
                    Level2 TEXT,
                    Level3 TEXT,
                    TagName TEXT NOT NULL,
                    IsSpecial INTEGER NOT NULL DEFAULT 0
                )";
            _ = cmd.ExecuteNonQuery();
        }

        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromResult(new AppDbContext(options)));

        return (seedingContext, factory, connection);
    }

    private static void InsertOldRow(SqliteConnection connection, int syncedItemId, string level1, string? level2, string? level3, bool isSpecial)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO SyncedItemClassifications (SyncedItemId, Level1, Level2, Level3, TagName, IsSpecial) VALUES (@sid, @l1, @l2, @l3, @tag, @special)";
        _ = cmd.Parameters.AddWithValue("@sid", syncedItemId);
        _ = cmd.Parameters.AddWithValue("@l1", level1);
        _ = cmd.Parameters.AddWithValue("@l2", (object?)level2 ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("@l3", (object?)level3 ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue("@tag", level3 ?? level2 ?? level1);
        _ = cmd.Parameters.AddWithValue("@special", isSpecial ? 1 : 0);
        _ = cmd.ExecuteNonQuery();
    }

    private static IClassificationDataMigrationService CreateSut(IDbContextFactory<AppDbContext> factory, ICategoryResolutionService? categoryResolutionService = null)
    {
        categoryResolutionService ??= Substitute.For<ICategoryResolutionService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ClassificationDataMigrationService>>();

        return new ClassificationDataMigrationService(factory, categoryResolutionService, logger);
    }

    [Fact]
    public async Task when_junction_table_already_populated_then_skips()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var _ = connection;

        var item = new SyncedItemEntity
        {
            AccountId = new AccountId("user-1"),
            RemoteItemId = new OneDriveItemId("remote-1"),
            RemotePath = "/file.txt",
            LocalPath = "/local/file.txt",
            RemoteModifiedAt = DateTimeOffset.UtcNow
        };
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var category = new FileClassificationCategoryEntity { Name = "Photos", Level = 1 };
        db.FileClassificationCategories.Add(category);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = item.Id, CategoryId = category.Id });
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        InsertOldRow(connection, item.Id, "Photos", null, null, false);

        var categoryResolutionService = Substitute.For<ICategoryResolutionService>();
        var sut = CreateSut(factory, categoryResolutionService);

        await sut.MigrateAsync(TestContext.Current.CancellationToken);

        await categoryResolutionService.DidNotReceive().ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_old_table_has_rows_then_migrates_all()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var _ = connection;

        var item1 = new SyncedItemEntity { AccountId = new AccountId("u"), RemoteItemId = new OneDriveItemId("r1"), RemotePath = "/a.jpg", LocalPath = "/local/a.jpg", RemoteModifiedAt = DateTimeOffset.UtcNow };
        var item2 = new SyncedItemEntity { AccountId = new AccountId("u"), RemoteItemId = new OneDriveItemId("r2"), RemotePath = "/b.jpg", LocalPath = "/local/b.jpg", RemoteModifiedAt = DateTimeOffset.UtcNow };
        db.SyncedItems.AddRange(item1, item2);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        InsertOldRow(connection, item1.Id, "Photos", null, null, false);
        InsertOldRow(connection, item2.Id, "Documents", null, null, false);

        var categoryResolutionService = Substitute.For<ICategoryResolutionService>();
        categoryResolutionService.ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>())
                                 .Returns(callInfo => Task.FromResult<IReadOnlyList<int>>([1]));
        var sut = CreateSut(factory, categoryResolutionService);

        await sut.MigrateAsync(TestContext.Current.CancellationToken);

        await categoryResolutionService.Received(2).ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_text_has_no_matching_category_then_creates_category()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var _ = connection;

        var item = new SyncedItemEntity { AccountId = new AccountId("u"), RemoteItemId = new OneDriveItemId("r1"), RemotePath = "/file.jpg", LocalPath = "/local/file.jpg", RemoteModifiedAt = DateTimeOffset.UtcNow };
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        InsertOldRow(connection, item.Id, "BrandNewCategory", null, null, false);

        var category = new FileClassificationCategoryEntity { Name = "BrandNewCategory", Level = 1 };
        db.FileClassificationCategories.Add(category);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var categoryResolutionService = Substitute.For<ICategoryResolutionService>();
        categoryResolutionService.ResolveManyAsync(Arg.Any<IReadOnlyList<FileClassification>>(), Arg.Any<CancellationToken>())
                                 .Returns(_ => Task.FromResult<IReadOnlyList<int>>([category.Id]));
        var sut = CreateSut(factory, categoryResolutionService);

        await sut.MigrateAsync(TestContext.Current.CancellationToken);

        var junctionRows = db.SyncedItemFileClassifications.Where(r => r.SyncedItemId == item.Id).ToList();
        junctionRows.ShouldHaveSingleItem();
        junctionRows[0].CategoryId.ShouldBe(category.Id);
    }
}
