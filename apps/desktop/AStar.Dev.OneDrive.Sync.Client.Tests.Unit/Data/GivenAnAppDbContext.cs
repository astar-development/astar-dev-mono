using AStar.Dev.OneDrive.Sync.Client.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data;

public sealed class GivenAnAppDbContext : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenAnAppDbContext()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        context = new AppDbContext(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    [Fact]
    public void when_querying_file_classification_categories_then_dbset_is_accessible()
    {
        var result = context.FileClassificationCategories.ToList();

        result.ShouldNotBeNull();
    }

    [Fact]
    public void when_querying_synced_item_file_classifications_then_dbset_is_accessible()
    {
        var result = context.SyncedItemFileClassifications.ToList();

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_file_classification_category_added_then_it_can_be_retrieved()
    {
        context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Photos", Level = 1 });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.FileClassificationCategories.First();

        retrieved.Name.ShouldBe("Photos");
    }

    [Fact]
    public async Task when_synced_item_file_classification_added_then_it_can_be_retrieved()
    {
        var account = new AccountEntity { Id = new AccountId("test-account"), Profile = AccountProfileFactory.Create("Test User", "test@test.com") };
        context.Accounts.Add(account);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var syncedItem = new SyncedItemEntity { AccountId = account.Id, RemoteItemId = new OneDriveItemId("item-1"), RemotePath = "/test/file.jpg", LocalPath = "/local/file.jpg" };
        context.SyncedItems.Add(syncedItem);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var category = new FileClassificationCategoryEntity { Name = "Photos", Level = 1 };
        context.FileClassificationCategories.Add(category);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = syncedItem.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.SyncedItemFileClassifications.First();

        retrieved.SyncedItemId.ShouldBe(syncedItem.Id);
        retrieved.CategoryId.ShouldBe(category.Id);
    }
}
