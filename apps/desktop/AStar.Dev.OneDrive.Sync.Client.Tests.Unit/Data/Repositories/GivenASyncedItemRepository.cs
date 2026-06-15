using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
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
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        var taggedItem = FileItem(remotePath: "/photo.jpg");
        var untaggedItem = FileItem(remotePath: "/doc.txt");
        db.SyncedItems.AddRange(taggedItem, untaggedItem);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.SyncedItemClassifications.Add(new SyncedItemClassificationEntity { SyncedItemId = taggedItem.Id, Level1 = "Image", TagName = "Image", IsSpecial = false });
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
        var (db, factory) = CreateInMemoryFactory();
        var repository = new SyncedItemRepository(factory);
        var item = FileItem(remotePath: "/photo.jpg");
        db.SyncedItems.Add(item);
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.SyncedItemClassifications.Add(new SyncedItemClassificationEntity { SyncedItemId = item.Id, Level1 = "Image", TagName = "Image", IsSpecial = false });
        db.SyncedItemClassifications.Add(new SyncedItemClassificationEntity { SyncedItemId = item.Id, Level1 = "Media", TagName = "Media", IsSpecial = false });
        _ = await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var criteria = SyncedItemSearchCriteriaFactory.Create(new AccountId("user-1"));

        var results = await repository.SearchAsync(criteria, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(1);
        results[0].TagNames.Count.ShouldBe(2);
        results[0].TagNames.ShouldContain("Image");
        results[0].TagNames.ShouldContain("Media");
    }
}
