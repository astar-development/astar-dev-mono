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

    private static AccountEntity AccountWithLocalRoot(string accountId = "user-1", string localRoot = "/local") => new()
    {
        Id = new AccountId(accountId),
        SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore(localRoot))
    };

    private static FileDetailEntity UnsyncedFileDetail(string directory, string fileName, long fileSize = 1024) => new()
    {
        FileName = new FileName(fileName),
        DirectoryName = new DirectoryName(directory),
        FileHandle = new FileHandle(directory + "/" + fileName),
        FileSize = fileSize
    };

    private static void Classify(AppDbContext db, FileDetailEntity fileDetail, params FileClassificationCategoryEntity[] categories)
    {
        foreach (var category in categories)
            db.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
    }

    private static FileDetailEntity AttachClassifiedFile(AppDbContext db, SyncedItemEntity item, params FileClassificationCategoryEntity[] categories)
    {
        var fileDetail = FileDetailFor(item.LocalPath);
        db.Files.Add(fileDetail);
        item.FileDetailId = fileDetail.Id;
        foreach (var category in categories)
            db.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });

        return fileDetail;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
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
    public async Task when_get_distinct_tag_names_is_called_then_tags_of_unsynced_files_under_account_root_are_included()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var landscapeCategory = new FileClassificationCategoryEntity { Name = "Landscape", Level = 1 };
        var unsyncedFile = UnsyncedFileDetail("/local/pics", "photo.jpg");
        db.Accounts.Add(AccountWithLocalRoot());
        db.FileClassificationCategories.Add(landscapeCategory);
        db.Files.Add(unsyncedFile);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Classify(db, unsyncedFile, landscapeCategory);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tags = await repository.GetDistinctTagNamesAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        tags.ShouldContain("Landscape");
    }

    [Fact]
    public async Task when_get_distinct_tag_names_is_called_then_tags_of_unsynced_files_outside_account_root_are_excluded()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var videoCategory = new FileClassificationCategoryEntity { Name = "Video", Level = 1 };
        var fileOutsideRoot = UnsyncedFileDetail("/elsewhere", "clip.mp4");
        db.Accounts.Add(AccountWithLocalRoot());
        db.FileClassificationCategories.Add(videoCategory);
        db.Files.Add(fileOutsideRoot);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Classify(db, fileOutsideRoot, videoCategory);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tags = await repository.GetDistinctTagNamesAsync(new AccountId("user-1"), TestContext.Current.CancellationToken);

        tags.ShouldNotContain("Video");
    }

    [Fact]
    public async Task when_search_is_called_then_unsynced_files_under_account_root_are_included()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        db.Accounts.Add(AccountWithLocalRoot());
        db.Files.Add(UnsyncedFileDetail("/local/pics", "photo.jpg", fileSize: 2048));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"));

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].IsSynced.ShouldBeFalse();
        results[0].LocalPath.ShouldBe("/local/pics/photo.jpg");
        results[0].SizeInBytes.ShouldBe(2048);
        results[0].FileDetailId.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_search_is_called_then_unsynced_files_outside_account_root_are_excluded()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        db.Accounts.Add(AccountWithLocalRoot());
        db.Files.Add(UnsyncedFileDetail("/elsewhere", "photo.jpg"));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"));

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_search_is_called_then_file_details_linked_to_synced_items_appear_only_as_synced_results()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var syncedItem = FileItem(remotePath: "/photo.jpg");
        db.Accounts.Add(AccountWithLocalRoot());
        db.SyncedItems.Add(syncedItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ = AttachClassifiedFile(db, syncedItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"));

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].IsSynced.ShouldBeTrue();
    }

    [Fact]
    public async Task when_search_is_called_with_name_fragment_then_unsynced_files_are_filtered_by_name()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        db.Accounts.Add(AccountWithLocalRoot());
        db.Files.Add(UnsyncedFileDetail("/local/docs", "report.pdf"));
        db.Files.Add(UnsyncedFileDetail("/local/pics", "holiday.jpg"));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), nameFragment: "report");

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].LocalPath.ShouldBe("/local/docs/report.pdf");
    }

    [Fact]
    public async Task when_search_is_called_with_tag_filter_then_unsynced_files_with_matching_tag_are_included()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var imageCategory = new FileClassificationCategoryEntity { Name = "Image", Level = 1 };
        var taggedFile = UnsyncedFileDetail("/local/pics", "photo.jpg");
        var untaggedFile = UnsyncedFileDetail("/local/docs", "notes.txt");
        db.Accounts.Add(AccountWithLocalRoot());
        db.FileClassificationCategories.Add(imageCategory);
        db.Files.AddRange(taggedFile, untaggedFile);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Classify(db, taggedFile, imageCategory);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), tags: ["Image"]);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].IsSynced.ShouldBeFalse();
        results[0].LocalPath.ShouldBe("/local/pics/photo.jpg");
        results[0].TagNames.ShouldContain("Image");
    }

    [Fact]
    public async Task when_search_is_called_with_duplicates_only_then_unsynced_duplicates_of_synced_files_are_included()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        db.Accounts.Add(AccountWithLocalRoot());
        db.SyncedItems.Add(FileItem(remotePath: "/docs/file.pdf", sizeInBytes: 2048));
        db.Files.Add(UnsyncedFileDetail("/local/backup", "file.pdf", fileSize: 2048));
        db.Files.Add(UnsyncedFileDetail("/local/backup", "unique.txt", fileSize: 999));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"), duplicatesOnly: true);

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(2);
        results.ShouldContain(r => r.IsSynced);
        results.ShouldContain(r => !r.IsSynced);
    }

    [Fact]
    public async Task when_search_is_called_and_account_has_no_local_root_then_only_synced_items_are_returned()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        db.SyncedItems.Add(FileItem(remotePath: "/synced.txt"));
        db.Files.Add(UnsyncedFileDetail("/local/pics", "photo.jpg"));
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"));

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].RemotePath.ShouldBe("/synced.txt");
    }

    [Fact]
    public async Task when_delete_file_detail_is_called_then_file_detail_and_classifications_are_removed()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var imageCategory = new FileClassificationCategoryEntity { Name = "Image", Level = 1 };
        var unsyncedFile = UnsyncedFileDetail("/local/pics", "photo.jpg");
        db.FileClassificationCategories.Add(imageCategory);
        db.Files.Add(unsyncedFile);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Classify(db, unsyncedFile, imageCategory);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.DeleteFileDetailAsync(unsyncedFile.Id, TestContext.Current.CancellationToken);

        db.ChangeTracker.Clear();
        db.Files.Count().ShouldBe(0);
        db.FileClassifications.Count().ShouldBe(0);
    }

    [Fact]
    public async Task when_delete_file_detail_is_called_then_synced_item_references_are_cleared()
    {
        var (db, factory, connection) = CreateSqliteFactory();
        await using var connectionScope = connection;
        var repository = new SyncedItemRepository(factory);
        var syncedItem = FileItem(remotePath: "/photo.jpg");
        db.SyncedItems.Add(syncedItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var fileDetail = AttachClassifiedFile(db, syncedItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.DeleteFileDetailAsync(fileDetail.Id, TestContext.Current.CancellationToken);

        db.ChangeTracker.Clear();
        db.Files.Count().ShouldBe(0);
        db.SyncedItems.Single().FileDetailId.ShouldBeNull();
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
