using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenASyncedItemFileClassificationEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenASyncedItemFileClassificationEntity()
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

    private async Task<SyncedItemEntity> CreateSyncedItemAsync(string remoteItemId = "item-1")
    {
        var account = new AccountEntity { Id = new AccountId($"account-for-{remoteItemId}"), Profile = AccountProfileFactory.Create("Test User", "test@test.com") };
        context.Accounts.Add(account);
        var syncedItem = new SyncedItemEntity { AccountId = account.Id, RemoteItemId = new OneDriveItemId(remoteItemId), RemotePath = "/test/file.jpg", LocalPath = "/local/file.jpg" };
        context.SyncedItems.Add(syncedItem);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return syncedItem;
    }

    private async Task<FileDetailEntity> CreateFileDetailAsync(string handle = "handle-1")
    {
        var fileDetail = new FileDetailEntity
        {
            FileName = new FileName("wallpaper.jpg"),
            DirectoryName = new DirectoryName("/pictures"),
            FileHandle = new FileHandle(handle),
            FileAccessDetail = new FileAccessDetailEntity { MoveRequired = true },
            ImageDetail = new ImageDetailEntity { Width = 1920, Height = 1080 },
            DeletionStatus = new DeletionStatusEntity()
        };
        context.Files.Add(fileDetail);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return fileDetail;
    }

    private async Task<FileClassificationCategoryEntity> CreateCategoryAsync(string name = "Animals")
    {
        var category = new FileClassificationCategoryEntity { Name = name, Level = 3 };
        context.FileClassificationCategories.Add(category);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return category;
    }

    [Fact]
    public void when_instantiated_then_id_defaults_to_zero() =>
        new SyncedItemFileClassificationEntity().Id.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_synced_item_id_defaults_to_null() =>
        new SyncedItemFileClassificationEntity().SyncedItemId.ShouldBeNull();

    [Fact]
    public void when_instantiated_then_file_detail_id_defaults_to_null() =>
        new SyncedItemFileClassificationEntity().FileDetailId.ShouldBeNull();

    [Fact]
    public void when_instantiated_then_category_id_defaults_to_zero() =>
        new SyncedItemFileClassificationEntity().CategoryId.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_synced_item_navigation_is_null() =>
        new SyncedItemFileClassificationEntity().SyncedItem.ShouldBeNull();

    [Fact]
    public void when_instantiated_then_file_detail_navigation_is_null() =>
        new SyncedItemFileClassificationEntity().FileDetail.ShouldBeNull();

    [Fact]
    public void when_instantiated_then_category_navigation_is_null() =>
        new SyncedItemFileClassificationEntity().Category.ShouldBeNull();

    [Fact]
    public void when_synced_item_id_is_set_then_it_reflects_in_the_property()
    {
        var entity = new SyncedItemFileClassificationEntity
        {
            SyncedItemId = 42
        };

        entity.SyncedItemId.ShouldBe(42);
    }

    [Fact]
    public void when_category_id_is_set_then_it_reflects_in_the_property()
    {
        var entity = new SyncedItemFileClassificationEntity
        {
            CategoryId = 7
        };

        entity.CategoryId.ShouldBe(7);
    }

    [Fact]
    public async Task when_a_synced_item_classification_is_added_then_it_can_be_retrieved_with_its_navigations()
    {
        var syncedItem = await CreateSyncedItemAsync();
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = syncedItem.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.SyncedItemFileClassifications.Include(c => c.Category).Include(c => c.SyncedItem).First();

        retrieved.Category!.Name.ShouldBe("Animals");
        retrieved.SyncedItem!.RemoteItemId.Id.ShouldBe("item-1");
    }

    [Fact]
    public async Task when_a_file_detail_classification_is_added_then_it_can_be_retrieved_with_its_navigations()
    {
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.SyncedItemFileClassifications.Include(c => c.Category).Include(c => c.FileDetail).First();

        retrieved.Category!.Name.ShouldBe("Animals");
        retrieved.FileDetail!.FileHandle.Value.ShouldBe("handle-1");
    }

    [Fact]
    public async Task when_the_same_synced_item_and_category_pair_is_added_twice_then_save_fails()
    {
        var syncedItem = await CreateSyncedItemAsync();
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = syncedItem.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = syncedItem.Id, CategoryId = category.Id });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_the_same_file_detail_and_category_pair_is_added_twice_then_save_fails()
    {
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_a_synced_item_and_a_file_detail_share_the_same_category_then_both_rows_are_saved()
    {
        var syncedItem = await CreateSyncedItemAsync();
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = syncedItem.Id, CategoryId = category.Id });
        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedItemFileClassifications.Count().ShouldBe(2);
    }

    [Fact]
    public async Task when_both_parents_are_set_then_save_fails()
    {
        var syncedItem = await CreateSyncedItemAsync();
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = syncedItem.Id, FileDetailId = fileDetail.Id, CategoryId = category.Id });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_neither_parent_is_set_then_save_fails()
    {
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { CategoryId = category.Id });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_a_category_referenced_by_a_classification_is_deleted_then_save_fails()
    {
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Should.Throw<InvalidOperationException>(() => context.FileClassificationCategories.Remove(category));
    }

    [Fact]
    public async Task when_the_classified_synced_item_is_deleted_then_its_classification_is_also_deleted()
    {
        var syncedItem = await CreateSyncedItemAsync();
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { SyncedItemId = syncedItem.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedItems.Remove(syncedItem);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedItemFileClassifications.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_classified_file_is_deleted_then_its_classification_is_also_deleted()
    {
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.SyncedItemFileClassifications.Add(new SyncedItemFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Files.Remove(fileDetail);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.SyncedItemFileClassifications.Any().ShouldBeFalse();
    }
}
