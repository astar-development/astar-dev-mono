using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
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
        db.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = taggedItem.Id, CategoryId = imageCategory.Id });
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
        db.SyncedItemFileClassifications.AddRange(
            new SyncedItemFileClassificationEntity { SyncedItemId = item.Id, CategoryId = imageCategory.Id },
            new SyncedItemFileClassificationEntity { SyncedItemId = item.Id, CategoryId = mediaCategory.Id });
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
        db.SyncedItemFileClassifications.AddRange(
            new SyncedItemFileClassificationEntity { SyncedItemId = item.Id, CategoryId = imageCategory.Id },
            new SyncedItemFileClassificationEntity { SyncedItemId = item.Id, CategoryId = mediaCategory.Id });
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
        db.SyncedItemFileClassifications.AddRange(
            new SyncedItemFileClassificationEntity { SyncedItemId = itemForAccountOne.Id, CategoryId = imageCategory.Id },
            new SyncedItemFileClassificationEntity { SyncedItemId = itemForAccountTwo.Id, CategoryId = videoCategory.Id });
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
        db.SyncedItemFileClassifications.AddRange(
            new SyncedItemFileClassificationEntity { SyncedItemId = firstItem.Id, CategoryId = imageCategory.Id },
            new SyncedItemFileClassificationEntity { SyncedItemId = secondItem.Id, CategoryId = imageCategory.Id });
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
    public async Task when_upsert_file_classifications_then_replaces_existing_rows()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var catA = new FileClassificationCategoryEntity { Name = "CatA", Level = 1 };
        var catB = new FileClassificationCategoryEntity { Name = "CatB", Level = 1 };
        var catOld = new FileClassificationCategoryEntity { Name = "CatOld", Level = 1 };
        var item = FileItem(remotePath: "/photo.jpg");
        db.FileClassificationCategories.AddRange(catA, catB, catOld);
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = item.Id, CategoryId = catOld.Id });
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.UpsertFileClassificationsAsync(item.Id, [catA.Id, catB.Id], TestContext.Current.CancellationToken);

        var rows = db.SyncedItemFileClassifications.Where(r => r.SyncedItemId == item.Id).ToList();
        rows.Count.ShouldBe(2);
        rows.ShouldContain(r => r.CategoryId == catA.Id);
        rows.ShouldContain(r => r.CategoryId == catB.Id);
        rows.ShouldNotContain(r => r.CategoryId == catOld.Id);
    }

    [Fact]
    public async Task when_upsert_file_classifications_with_empty_list_then_existing_rows_are_deleted()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var cat = new FileClassificationCategoryEntity { Name = "SomeCat", Level = 1 };
        var item = FileItem(remotePath: "/photo.jpg");
        db.FileClassificationCategories.Add(cat);
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = item.Id, CategoryId = cat.Id });
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.UpsertFileClassificationsAsync(item.Id, [], TestContext.Current.CancellationToken);

        var rows = db.SyncedItemFileClassifications.Where(r => r.SyncedItemId == item.Id).ToList();
        rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_searching_by_tag_then_returns_items_with_matching_junction_row()
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
        db.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = taggedItem.Id, CategoryId = imageCategory.Id });
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), tags: ["Image"]);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/photo.jpg");
    }

    [Fact]
    public async Task when_getting_distinct_tag_names_then_reads_from_junction_table()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var imageCategory = new FileClassificationCategoryEntity { Name = "Image", Level = 1 };
        var videoCategory = new FileClassificationCategoryEntity { Name = "Video", Level = 1 };
        var item = FileItem(remotePath: "/photo.jpg");
        db.FileClassificationCategories.AddRange(imageCategory, videoCategory);
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.SyncedItemFileClassifications.AddRange(
            new SyncedItemFileClassificationEntity { SyncedItemId = item.Id, CategoryId = imageCategory.Id },
            new SyncedItemFileClassificationEntity { SyncedItemId = item.Id, CategoryId = videoCategory.Id });
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tags = await repository.GetDistinctTagNamesAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        tags.Count.ShouldBe(2);
        tags.ShouldContain("Image");
        tags.ShouldContain("Video");
    }

    [Fact]
    public async Task when_upsert_with_classifications_is_called_for_new_item_then_item_and_classifications_are_persisted()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var catA = new FileClassificationCategoryEntity { Name = "Image", Level = 1 };
        var catB = new FileClassificationCategoryEntity { Name = "Media", Level = 1 };
        db.FileClassificationCategories.AddRange(catA, catB);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var newItem = FileItem(remotePath: "/photo.jpg");

        var syncedItemId = await repository.UpsertWithClassificationsAsync(newItem, [catA.Id, catB.Id], TestContext.Current.CancellationToken);

        var persistedItem = db.SyncedItems.FirstOrDefault(i => i.Id == syncedItemId);
        persistedItem.ShouldNotBeNull();
        persistedItem.RemotePath.ShouldBe("/photo.jpg");
        var rows = db.SyncedItemFileClassifications.Where(r => r.SyncedItemId == syncedItemId).ToList();
        rows.Count.ShouldBe(2);
        rows.ShouldContain(r => r.CategoryId == catA.Id);
        rows.ShouldContain(r => r.CategoryId == catB.Id);
    }

    [Fact]
    public async Task when_upsert_with_classifications_is_called_for_existing_item_then_item_is_updated_and_classifications_replaced()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var catOld = new FileClassificationCategoryEntity { Name = "OldCat", Level = 1 };
        var catNew = new FileClassificationCategoryEntity { Name = "NewCat", Level = 1 };
        var existingItem = FileItem(remotePath: "/original.jpg");
        db.FileClassificationCategories.AddRange(catOld, catNew);
        db.SyncedItems.Add(existingItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = existingItem.Id, CategoryId = catOld.Id });
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var updatedItem = new SyncedItemEntity
        {
            AccountId = existingItem.AccountId,
            RemoteItemId = existingItem.RemoteItemId,
            RemotePath = "/updated.jpg",
            LocalPath = existingItem.LocalPath,
            IsFolder = existingItem.IsFolder,
            RemoteModifiedAt = DateTimeOffset.UtcNow,
            SizeInBytes = existingItem.SizeInBytes
        };

        var syncedItemId = await repository.UpsertWithClassificationsAsync(updatedItem, [catNew.Id], TestContext.Current.CancellationToken);

        db.ChangeTracker.Clear();
        var persistedItem = db.SyncedItems.Find(syncedItemId);
        persistedItem.ShouldNotBeNull();
        persistedItem.RemotePath.ShouldBe("/updated.jpg");
        var rows = db.SyncedItemFileClassifications.Where(r => r.SyncedItemId == syncedItemId).ToList();
        rows.Count.ShouldBe(1);
        rows[0].CategoryId.ShouldBe(catNew.Id);
    }

    [Fact]
    public async Task when_upsert_with_classifications_is_called_with_empty_category_list_then_item_is_persisted_with_no_classifications()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var catOld = new FileClassificationCategoryEntity { Name = "OldCat", Level = 1 };
        var existingItem = FileItem(remotePath: "/file.txt");
        db.FileClassificationCategories.Add(catOld);
        db.SyncedItems.Add(existingItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = existingItem.Id, CategoryId = catOld.Id });
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var updatedItem = new SyncedItemEntity
        {
            AccountId = existingItem.AccountId,
            RemoteItemId = existingItem.RemoteItemId,
            RemotePath = existingItem.RemotePath,
            LocalPath = existingItem.LocalPath,
            IsFolder = existingItem.IsFolder,
            RemoteModifiedAt = DateTimeOffset.UtcNow,
            SizeInBytes = existingItem.SizeInBytes
        };

        var syncedItemId = await repository.UpsertWithClassificationsAsync(updatedItem, [], TestContext.Current.CancellationToken);

        var rows = db.SyncedItemFileClassifications.Where(r => r.SyncedItemId == syncedItemId).ToList();
        rows.ShouldBeEmpty();
    }
}
