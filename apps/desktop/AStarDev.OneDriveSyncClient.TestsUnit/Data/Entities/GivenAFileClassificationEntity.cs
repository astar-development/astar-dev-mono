using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Data.Entities;

public sealed class GivenAFileClassificationEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;
    private bool disposed;

    public GivenAFileClassificationEntity()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        context = new AppDbContext(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        context.Dispose();
        connection.Dispose();
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
        new FileClassificationEntity().Id.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_file_detail_id_defaults_to_empty() =>
        new FileClassificationEntity().FileDetailId.ShouldBe(default(FileId));

    [Fact]
    public void when_instantiated_then_category_id_defaults_to_zero() =>
        new FileClassificationEntity().CategoryId.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_file_detail_navigation_is_null() =>
        new FileClassificationEntity().FileDetail.ShouldBeNull();

    [Fact]
    public void when_instantiated_then_category_navigation_is_null() =>
        new FileClassificationEntity().Category.ShouldBeNull();

    [Fact]
    public void when_category_id_is_set_then_it_reflects_in_the_property()
    {
        var entity = new FileClassificationEntity
        {
            CategoryId = 7
        };

        entity.CategoryId.ShouldBe(7);
    }

    [Fact]
    public async Task when_a_classification_is_added_then_it_can_be_retrieved_with_its_navigations()
    {
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.FileClassifications.Include(c => c.Category).Include(c => c.FileDetail).First();

        retrieved.Category!.Name.ShouldBe("Animals");
        retrieved.FileDetail!.FileHandle.Value.ShouldBe("handle-1");
    }

    [Fact]
    public async Task when_the_same_file_and_category_pair_is_added_twice_then_save_fails()
    {
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_two_files_share_the_same_category_then_both_rows_are_saved()
    {
        var firstFile = await CreateFileDetailAsync("handle-1");
        var secondFile = await CreateFileDetailAsync("handle-2");
        var category = await CreateCategoryAsync();

        context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = firstFile.Id, CategoryId = category.Id });
        context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = secondFile.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassifications.Count().ShouldBe(2);
    }

    [Fact]
    public async Task when_a_category_referenced_by_a_classification_is_deleted_then_save_fails()
    {
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Should.Throw<InvalidOperationException>(() => context.FileClassificationCategories.Remove(category));
    }

    [Fact]
    public async Task when_the_classified_file_is_deleted_then_its_classification_is_also_deleted()
    {
        var fileDetail = await CreateFileDetailAsync();
        var category = await CreateCategoryAsync();

        context.FileClassifications.Add(new FileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Files.Remove(fileDetail);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.FileClassifications.Any().ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_synced_item_is_linked_to_a_file_detail_then_the_link_persists()
    {
        var fileDetail = await CreateFileDetailAsync();
        var account = new AccountEntity { Id = new AccountId("account-link"), Profile = AccountProfileFactory.Create("Test User", "test@test.com") };
        context.Accounts.Add(account);
        var syncedItem = new SyncedItemEntity { AccountId = account.Id, RemoteItemId = new OneDriveItemId("item-link"), RemotePath = "/pictures/wallpaper.jpg", LocalPath = "/pictures/wallpaper.jpg", FileDetailId = fileDetail.Id };
        context.SyncedItems.Add(syncedItem);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.ChangeTracker.Clear();
        var retrieved = context.SyncedItems.Single(i => i.Id == syncedItem.Id);

        retrieved.FileDetailId.ShouldBe(fileDetail.Id);
    }

    [Fact]
    public void when_a_synced_item_is_instantiated_then_file_detail_id_defaults_to_null() =>
        new SyncedItemEntity().FileDetailId.ShouldBeNull();
}
