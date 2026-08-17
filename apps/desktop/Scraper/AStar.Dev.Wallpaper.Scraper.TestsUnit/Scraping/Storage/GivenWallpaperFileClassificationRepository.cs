using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AStar.Dev.Wallpaper.Scraper.Scraping.ImageDownload;
using AStar.Dev.Wallpaper.Scraper.Scraping.Storage;
using AStar.Dev.Wallpaper.Scraper.Scraping.Tags;

namespace AStar.Dev.Wallpaper.Scraper.TestsUnit.Scraping.Storage;

public sealed class GivenWallpaperFileClassificationRepository : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;
    private bool disposed;

    public GivenWallpaperFileClassificationRepository()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_no_file_exists_with_the_given_name_then_is_already_downloaded_is_false()
    {
        var sut = new WallpaperFileClassificationRepository(dbContextFactory);

        bool alreadyDownloaded = await sut.IsAlreadyDownloadedAsync("pic.jpg", TestContext.Current.CancellationToken);

        alreadyDownloaded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_file_exists_whose_name_contains_the_given_text_then_is_already_downloaded_is_true()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.Files.Add(new FileDetailEntity
            {
                FileName = new FileName("wallhaven-abc123.jpg"),
                DirectoryName = new DirectoryName("/wallpapers/nature"),
                FileHandle = new FileHandle(Guid.NewGuid().ToString()),
            });
            context.SaveChanges();
        }

        var sut = new WallpaperFileClassificationRepository(dbContextFactory);

        bool alreadyDownloaded = await sut.IsAlreadyDownloadedAsync("abc123", TestContext.Current.CancellationToken);

        alreadyDownloaded.ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_file_with_a_matching_name_exists_in_any_directory_then_is_already_downloaded_is_true()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.Files.Add(new FileDetailEntity
            {
                FileName = new FileName("wallhaven-abc123.jpg"),
                DirectoryName = new DirectoryName("/wallpapers/some/other/directory"),
                FileHandle = new FileHandle(Guid.NewGuid().ToString()),
            });
            context.SaveChanges();
        }

        var sut = new WallpaperFileClassificationRepository(dbContextFactory);

        bool alreadyDownloaded = await sut.IsAlreadyDownloadedAsync("abc123", TestContext.Current.CancellationToken);

        alreadyDownloaded.ShouldBeTrue();
    }

    [Fact]
    public async Task when_no_file_name_contains_the_given_text_then_is_already_downloaded_is_false()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.Files.Add(new FileDetailEntity
            {
                FileName = new FileName("wallhaven-abc123.jpg"),
                DirectoryName = new DirectoryName("/wallpapers/nature"),
                FileHandle = new FileHandle(Guid.NewGuid().ToString()),
            });
            context.SaveChanges();
        }

        var sut = new WallpaperFileClassificationRepository(dbContextFactory);

        bool alreadyDownloaded = await sut.IsAlreadyDownloadedAsync("def456", TestContext.Current.CancellationToken);

        alreadyDownloaded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_wallpaper_with_multiple_tags_is_recorded_then_a_file_classification_is_created_per_tag()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Nature", });
            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Outdoors", });
            context.SaveChanges();
        }

        var sut = new WallpaperFileClassificationRepository(dbContextFactory);
        List<TagData> tags = [new("Nature", "outdoors"), new("Outdoors", "outdoors")];

        await sut.RecordAsync(tags, "https://wallhaven.cc/images/pic.jpg", "/wallpapers/nature", 1234, new ImageDimensions(100, 200), TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        var classifications = verifyContext.FileClassifications.Include(c => c.FileDetail).ThenInclude(d => d!.ImageDetail).Include(c => c.Category).ToList();
        classifications.Count.ShouldBe(2);
        classifications.ShouldAllBe(c => c.FileDetail!.FileName.Value == "pic.jpg");
        classifications.ShouldAllBe(c => c.FileDetail!.DirectoryName.Value == "/wallpapers/nature");
        classifications.ShouldAllBe(c => c.FileDetail!.FileSize == 1234);
        classifications.ShouldAllBe(c => c.FileDetail!.ImageDetail.Width == 100 && c.FileDetail!.ImageDetail.Height == 200);
        classifications.Select(c => c.Category!.Name).ShouldBe(["Nature", "Outdoors"], ignoreOrder: true);
    }

    [Fact]
    public async Task when_a_wallpaper_is_recorded_then_the_user_managed_search_categories_are_never_written()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            var searchConfiguration = new SearchConfigurationEntity();
            searchConfiguration.SearchCategories.Add(new SearchCategoryEntity { Id = "landscapes-id", Name = "Landscapes", });
            context.ScrapeConfiguration.Add(new ScrapeConfigurationEntity { SearchConfiguration = searchConfiguration });
            context.FileClassificationCategories.Add(new FileClassificationCategoryEntity { Name = "Nature", });
            context.SaveChanges();
        }

        var sut = new WallpaperFileClassificationRepository(dbContextFactory);

        await sut.RecordAsync([TagDataFactory.Create("Nature", "outdoors")], "https://wallhaven.cc/images/pic.jpg", "/wallpapers/nature", 1234, new ImageDimensions(100, 200), TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.FileClassifications.Count().ShouldBe(1);
        var searchCategory = verifyContext.SearchCategories.Single();
        searchCategory.Id.ShouldBe("landscapes-id");
        searchCategory.Name.ShouldBe("Landscapes");
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

        if (disposing)
            connection.Dispose();
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
