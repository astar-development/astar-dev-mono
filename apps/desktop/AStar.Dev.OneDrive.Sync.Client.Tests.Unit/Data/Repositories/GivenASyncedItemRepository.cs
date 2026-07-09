using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Repositories;

public sealed class GivenASyncedItemRepository
{
    private static SyncedItemEntity FileItem(string accountId = "user-1", string remotePath = "/file.txt", long? sizeInBytes = 1024, bool isFolder = false) => new()
    {
        AccountId = new AccountId(accountId),
        RemoteItemId = new OneDriveItemId(Guid.NewGuid().ToString()),
        RemotePath = remotePath,
        LocalPath = "/local" + remotePath,
        IsFolder = isFolder,
        RemoteModifiedAt = DateTimeOffset.UtcNow,
        SizeInBytes = sizeInBytes
    };

    private static FileDetailEntity FileDetailFor(string localPath) => new()
    {
        FileName = new FileName(localPath[(localPath.LastIndexOf('/') + 1)..]),
        DirectoryName = new DirectoryName(localPath[..localPath.LastIndexOf('/')]),
        FileHandle = new FileHandle(localPath)
    };

    private static FileDetailEntity AttachClassifiedFile(AppDbContext db, SyncedItemEntity item, params FileClassificationCategoryEntity[] categories)
    {
        var fileDetail = FileDetailFor(item.LocalPath);
        db.Files.Add(fileDetail);
        item.FileDetailId = fileDetail.Id;
        foreach (var category in categories)
            db.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });

        return fileDetail;
    }

    private static (AppDbContext seedingContext, IDbContextFactory<AppDbContext> factory) CreateInMemoryFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        return (seedingContext, factory);
    }

    private static (AppDbContext seedingContext, IDbContextFactory<AppDbContext> factory, SqliteConnection connection) CreateSqliteFactory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            _ = cmd.ExecuteNonQuery();
        }
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        return (seedingContext, factory, connection);
    }

    [Fact]
    public async Task when_search_is_called_with_name_fragment_then_only_matching_items_are_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.Add(FileItem(remotePath: "/docs/report.pdf"));
        db.SyncedItems.Add(FileItem(remotePath: "/photos/holiday.jpg"));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), nameFragment: "report");

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/docs/report.pdf");
    }

    [Fact]
    public async Task when_search_is_called_with_min_bytes_then_items_below_threshold_are_excluded()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.Add(FileItem(remotePath: "/small.txt", sizeInBytes: 100));
        db.SyncedItems.Add(FileItem(remotePath: "/large.bin", sizeInBytes: 5000));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), minBytes: 1000);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/large.bin");
    }

    [Fact]
    public async Task when_search_is_called_with_max_bytes_then_items_above_threshold_are_excluded()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.Add(FileItem(remotePath: "/small.txt", sizeInBytes: 100));
        db.SyncedItems.Add(FileItem(remotePath: "/large.bin", sizeInBytes: 5000));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), maxBytes: 500);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/small.txt");
    }

    [Fact]
    public async Task when_search_is_called_with_size_filter_and_item_has_null_size_then_item_is_excluded()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.Add(FileItem(remotePath: "/unknown-size.bin", sizeInBytes: null));
        db.SyncedItems.Add(FileItem(remotePath: "/known.txt", sizeInBytes: 500));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), minBytes: 100);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/known.txt");
    }

    [Fact]
    public async Task when_search_is_called_with_tag_filter_then_only_tagged_items_are_returned()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var imageCategory = new FileClassificationCategoryEntity { Name = "Image", Level = 1 };
        var taggedItem = FileItem(remotePath: "/photo.jpg");
        var untaggedItem = FileItem(remotePath: "/doc.txt");
        db.FileClassificationCategories.Add(imageCategory);
        db.SyncedItems.AddRange(taggedItem, untaggedItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ = AttachClassifiedFile(db, taggedItem, imageCategory);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), tags: ["Image"]);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/photo.jpg");
    }

    [Fact]
    public async Task when_search_is_called_with_duplicates_only_then_only_duplicate_files_are_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.Add(FileItem(remotePath: "/docs/file.pdf", sizeInBytes: 2048));
        db.SyncedItems.Add(FileItem(remotePath: "/backup/file.pdf", sizeInBytes: 2048));
        db.SyncedItems.Add(FileItem(remotePath: "/unique.txt", sizeInBytes: 999));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(2);
        results.ShouldAllBe(r => r.RemotePath.EndsWith("file.pdf"));
    }

    [Fact]
    public async Task when_search_is_called_then_folders_are_always_excluded()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.Add(FileItem(remotePath: "/docs", isFolder: true));
        db.SyncedItems.Add(FileItem(remotePath: "/docs/file.txt", isFolder: false));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"));

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/docs/file.txt");
    }

    [Fact]
    public async Task when_search_is_called_with_combined_name_and_size_criteria_then_both_filters_apply()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.Add(FileItem(remotePath: "/docs/report.pdf", sizeInBytes: 5000));
        db.SyncedItems.Add(FileItem(remotePath: "/docs/summary.pdf", sizeInBytes: 100));
        db.SyncedItems.Add(FileItem(remotePath: "/photos/photo.jpg", sizeInBytes: 5000));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), nameFragment: ".pdf", minBytes: 1000);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/docs/report.pdf");
    }

    [Fact]
    public async Task when_search_is_called_then_tag_names_are_populated_in_result()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var imageCategory = new FileClassificationCategoryEntity { Name = "Image", Level = 1 };
        var mediaCategory = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        var item = FileItem(remotePath: "/photo.jpg");
        db.FileClassificationCategories.AddRange(imageCategory, mediaCategory);
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ = AttachClassifiedFile(db, item, imageCategory, mediaCategory);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"));

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].TagNames.Count.ShouldBe(2);
        results[0].TagNames.ShouldContain("Image");
        results[0].TagNames.ShouldContain("Media");
    }

    [Fact]
    public async Task when_get_distinct_tag_names_is_called_then_distinct_tags_for_account_are_returned()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var imageCategory = new FileClassificationCategoryEntity { Name = "Image", Level = 1 };
        var mediaCategory = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        var item = FileItem(remotePath: "/photo.jpg");
        db.FileClassificationCategories.AddRange(imageCategory, mediaCategory);
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ = AttachClassifiedFile(db, item, imageCategory, mediaCategory);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tags = await repository.GetDistinctTagNamesAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        tags.Count.ShouldBe(2);
        tags.ShouldContain("Image");
        tags.ShouldContain("Media");
    }

    [Fact]
    public async Task when_get_distinct_tag_names_is_called_for_account_with_no_classifications_then_empty_list_is_returned()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.Add(FileItem(remotePath: "/file.txt"));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tags = await repository.GetDistinctTagNamesAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        tags.ShouldNotBeNull();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_get_distinct_tag_names_is_called_then_only_tags_for_requested_account_are_returned()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var imageCategory = new FileClassificationCategoryEntity { Name = "Image", Level = 1 };
        var videoCategory = new FileClassificationCategoryEntity { Name = "Video", Level = 1 };
        var itemForAccountOne = FileItem(accountId: "user-1", remotePath: "/photo.jpg");
        var itemForAccountTwo = FileItem(accountId: "user-2", remotePath: "/video.mp4");
        db.FileClassificationCategories.AddRange(imageCategory, videoCategory);
        db.SyncedItems.AddRange(itemForAccountOne, itemForAccountTwo);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ = AttachClassifiedFile(db, itemForAccountOne, imageCategory);
        _ = AttachClassifiedFile(db, itemForAccountTwo, videoCategory);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tags = await repository.GetDistinctTagNamesAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        tags.Count.ShouldBe(1);
        tags.ShouldContain("Image");
        tags.ShouldNotContain("Video");
    }

    [Fact]
    public async Task when_get_distinct_tag_names_is_called_and_multiple_files_share_the_same_tag_then_tag_appears_once()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var imageCategory = new FileClassificationCategoryEntity { Name = "Image", Level = 1 };
        var firstItem = FileItem(remotePath: "/photo1.jpg");
        var secondItem = FileItem(remotePath: "/photo2.jpg");
        db.FileClassificationCategories.Add(imageCategory);
        db.SyncedItems.AddRange(firstItem, secondItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ = AttachClassifiedFile(db, firstItem, imageCategory);
        _ = AttachClassifiedFile(db, secondItem, imageCategory);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tags = await repository.GetDistinctTagNamesAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        tags.Count.ShouldBe(1);
        tags[0].ShouldBe("Image");
    }

    [Fact]
    public async Task when_search_sort_order_is_name_ascending_then_results_are_ordered_a_to_z()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.AddRange(
            FileItem(remotePath: "/files/bravo.txt", sizeInBytes: 2000),
            FileItem(remotePath: "/files/alpha.txt", sizeInBytes: 3000),
            FileItem(remotePath: "/files/charlie.txt", sizeInBytes: 1000));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), sortOrder: SearchSortOrder.NameAscending);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
        results[0].RemotePath.ShouldBe("/files/alpha.txt");
        results[1].RemotePath.ShouldBe("/files/bravo.txt");
        results[2].RemotePath.ShouldBe("/files/charlie.txt");
    }

    [Fact]
    public async Task when_search_sort_order_is_name_descending_then_results_are_ordered_z_to_a()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.AddRange(
            FileItem(remotePath: "/files/bravo.txt", sizeInBytes: 2000),
            FileItem(remotePath: "/files/alpha.txt", sizeInBytes: 3000),
            FileItem(remotePath: "/files/charlie.txt", sizeInBytes: 1000));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), sortOrder: SearchSortOrder.NameDescending);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
        results[0].RemotePath.ShouldBe("/files/charlie.txt");
        results[1].RemotePath.ShouldBe("/files/bravo.txt");
        results[2].RemotePath.ShouldBe("/files/alpha.txt");
    }

    [Fact]
    public async Task when_search_sort_order_is_size_ascending_then_results_are_ordered_smallest_first()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.AddRange(
            FileItem(remotePath: "/files/bravo.txt", sizeInBytes: 2000),
            FileItem(remotePath: "/files/alpha.txt", sizeInBytes: 3000),
            FileItem(remotePath: "/files/charlie.txt", sizeInBytes: 1000));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), sortOrder: SearchSortOrder.SizeAscending);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
        results[0].RemotePath.ShouldBe("/files/charlie.txt");
        results[1].RemotePath.ShouldBe("/files/bravo.txt");
        results[2].RemotePath.ShouldBe("/files/alpha.txt");
    }

    [Fact]
    public async Task when_search_sort_order_is_size_descending_then_results_are_ordered_largest_first()
    {
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.AddRange(
            FileItem(remotePath: "/files/bravo.txt", sizeInBytes: 2000),
            FileItem(remotePath: "/files/alpha.txt", sizeInBytes: 3000),
            FileItem(remotePath: "/files/charlie.txt", sizeInBytes: 1000));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), sortOrder: SearchSortOrder.SizeDescending);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
        results[0].RemotePath.ShouldBe("/files/alpha.txt");
        results[1].RemotePath.ShouldBe("/files/bravo.txt");
        results[2].RemotePath.ShouldBe("/files/charlie.txt");
    }

    [Fact]
    public async Task when_upsert_is_called_for_a_new_item_then_the_item_is_persisted_with_its_file_detail_link()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var fileDetail = FileDetailFor("/local/photo.jpg");
        db.Files.Add(fileDetail);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var newItem = FileItem(remotePath: "/photo.jpg");
        newItem.FileDetailId = fileDetail.Id;

        int syncedItemId = await repository.UpsertAsync(newItem, TestContext.Current.CancellationToken);

        db.ChangeTracker.Clear();
        var persistedItem = db.SyncedItems.Single(i => i.Id == syncedItemId);
        persistedItem.RemotePath.ShouldBe("/photo.jpg");
        persistedItem.FileDetailId.ShouldBe(fileDetail.Id);
    }

    [Fact]
    public async Task when_upsert_is_called_for_an_existing_item_then_the_file_detail_link_is_updated()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var existingItem = FileItem(remotePath: "/original.jpg");
        db.SyncedItems.Add(existingItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var fileDetail = FileDetailFor(existingItem.LocalPath);
        db.Files.Add(fileDetail);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var updatedItem = new SyncedItemEntity
        {
            AccountId = existingItem.AccountId,
            RemoteItemId = existingItem.RemoteItemId,
            RemotePath = "/updated.jpg",
            LocalPath = existingItem.LocalPath,
            IsFolder = existingItem.IsFolder,
            RemoteModifiedAt = DateTimeOffset.UtcNow,
            SizeInBytes = existingItem.SizeInBytes,
            FileDetailId = fileDetail.Id
        };

        int syncedItemId = await repository.UpsertAsync(updatedItem, TestContext.Current.CancellationToken);

        db.ChangeTracker.Clear();
        var persistedItem = db.SyncedItems.Single(i => i.Id == syncedItemId);
        persistedItem.RemotePath.ShouldBe("/updated.jpg");
        persistedItem.FileDetailId.ShouldBe(fileDetail.Id);
    }

    [Fact]
    public async Task when_delete_many_is_called_with_zero_ids_then_no_items_are_deleted()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var item = FileItem(remotePath: "/file.txt");
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.DeleteManyByRemoteIdAsync(new AccountId("user-1"), [], TestContext.Current.CancellationToken);

        db.SyncedItems.Count().ShouldBe(1);
    }

    [Fact]
    public async Task when_delete_many_is_called_with_single_id_then_that_item_is_deleted()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var item = FileItem(remotePath: "/file.txt");
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.DeleteManyByRemoteIdAsync(new AccountId("user-1"), [item.RemoteItemId], TestContext.Current.CancellationToken);

        db.SyncedItems.Count().ShouldBe(0);
    }

    [Fact]
    public async Task when_delete_many_is_called_with_more_than_200_ids_then_all_matching_items_are_deleted()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var items = Enumerable.Range(1, 250).Select(index => FileItem(remotePath: $"/file{index}.txt")).ToList();
        db.SyncedItems.AddRange(items);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var idsToDelete = items.Select(item => item.RemoteItemId).ToList();

        await repository.DeleteManyByRemoteIdAsync(new AccountId("user-1"), idsToDelete, TestContext.Current.CancellationToken);

        db.SyncedItems.Count().ShouldBe(0);
    }

    [Fact]
    public async Task when_delete_many_is_called_then_only_matching_account_items_are_deleted()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var itemForAccountOne = FileItem(accountId: "user-1", remotePath: "/file.txt");
        var itemForAccountTwo = FileItem(accountId: "user-2", remotePath: "/other.txt");
        db.SyncedItems.AddRange(itemForAccountOne, itemForAccountTwo);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.DeleteManyByRemoteIdAsync(new AccountId("user-1"), [itemForAccountOne.RemoteItemId], TestContext.Current.CancellationToken);

        db.SyncedItems.Count().ShouldBe(1);
        db.SyncedItems.Single().AccountId.ShouldBe(new AccountId("user-2"));
    }
}
