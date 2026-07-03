using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenADownloadedFileClassificationEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;

    public GivenADownloadedFileClassificationEntity()
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

    private static FileDetailEntity CreateFileDetail(string handle = "handle-1") =>
        new()
        {
            FileName = new FileName("wallpaper.jpg"),
            DirectoryName = new DirectoryName("/pictures"),
            FileHandle = new FileHandle(handle),
            FileAccessDetail = new FileAccessDetailEntity { MoveRequired = true },
            ImageDetail = new ImageDetailEntity { Width = 1920, Height = 1080 },
            DeletionStatus = new DeletionStatusEntity()
        };

    [Fact]
    public void when_instantiated_then_id_defaults_to_zero() =>
        new DownloadedFileClassificationEntity().Id.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_category_id_defaults_to_zero() =>
        new DownloadedFileClassificationEntity().CategoryId.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_category_navigation_is_null() =>
        new DownloadedFileClassificationEntity().Category.ShouldBeNull();

    [Fact]
    public void when_instantiated_then_file_detail_navigation_is_null() =>
        new DownloadedFileClassificationEntity().FileDetail.ShouldBeNull();

    [Fact]
    public void when_category_id_is_set_then_it_reflects_in_the_property()
    {
        var entity = new DownloadedFileClassificationEntity
        {
            CategoryId = 7
        };

        entity.CategoryId.ShouldBe(7);
    }

    [Fact]
    public async Task when_a_downloaded_file_classification_is_added_then_it_can_be_retrieved()
    {
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3 };
        var fileDetail = CreateFileDetail();
        context.FileClassificationCategories.Add(category);
        context.Files.Add(fileDetail);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.DownloadedFileClassifications.Add(new DownloadedFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.DownloadedFileClassifications.Include(d => d.Category).Include(d => d.FileDetail).First();

        retrieved.Category!.Name.ShouldBe("Animals");
        retrieved.FileDetail!.FileHandle.Value.ShouldBe("handle-1");
    }

    [Fact]
    public async Task when_the_same_file_and_category_pair_is_added_twice_then_save_fails()
    {
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3 };
        var fileDetail = CreateFileDetail();
        context.FileClassificationCategories.Add(category);
        context.Files.Add(fileDetail);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.DownloadedFileClassifications.Add(new DownloadedFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.DownloadedFileClassifications.Add(new DownloadedFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });

        await Should.ThrowAsync<DbUpdateException>(async () => await context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_a_category_referenced_by_a_downloaded_file_classification_is_deleted_then_save_fails()
    {
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3 };
        var fileDetail = CreateFileDetail();
        context.FileClassificationCategories.Add(category);
        context.Files.Add(fileDetail);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.DownloadedFileClassifications.Add(new DownloadedFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Should.Throw<InvalidOperationException>(() => context.FileClassificationCategories.Remove(category));
    }

    [Fact]
    public async Task when_the_classified_file_is_deleted_then_its_downloaded_file_classification_is_also_deleted()
    {
        var category = new FileClassificationCategoryEntity { Name = "Animals", Level = 3 };
        var fileDetail = CreateFileDetail();
        context.FileClassificationCategories.Add(category);
        context.Files.Add(fileDetail);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.DownloadedFileClassifications.Add(new DownloadedFileClassificationEntity { FileDetailId = fileDetail.Id, CategoryId = category.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Files.Remove(fileDetail);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.DownloadedFileClassifications.Any().ShouldBeFalse();
    }
}
