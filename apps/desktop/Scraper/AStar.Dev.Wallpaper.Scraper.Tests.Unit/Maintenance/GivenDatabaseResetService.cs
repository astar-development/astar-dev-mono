using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Maintenance;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Maintenance;

public sealed class GivenDatabaseResetService : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;
    private readonly MockFileSystem fileSystem = new();

    public GivenDatabaseResetService()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_reset_database_async_executes_then_all_file_detail_rows_are_removed()
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

        var sut = CreateSut();

        await sut.ResetDatabaseAsync(TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.Files.Count().ShouldBe(0);
    }

    [Fact]
    public async Task when_reset_database_async_executes_then_all_file_access_detail_rows_are_removed()
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

        var sut = CreateSut();

        await sut.ResetDatabaseAsync(TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.FileAccessDetails.Count().ShouldBe(0);
    }

    [Fact]
    public async Task when_reset_database_async_executes_then_all_image_detail_rows_are_removed()
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

        var sut = CreateSut();

        await sut.ResetDatabaseAsync(TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.Set<ImageDetailEntity>().Count().ShouldBe(0);
    }

    [Fact]
    public async Task when_reset_database_async_executes_then_every_search_category_progress_is_reset_to_zero()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            var searchConfiguration = new SearchConfigurationEntity();
            searchConfiguration.SearchCategories.Add(new SearchCategoryEntity
            {
                Id = "nature-id",
                Name = "Nature",
                LastKnownImageCount = 120,
                LastPageVisited = 3,
            });
            context.ScrapeConfiguration.Add(new ScrapeConfigurationEntity { SearchConfiguration = searchConfiguration });
            context.SaveChanges();
        }

        var sut = CreateSut();

        await sut.ResetDatabaseAsync(TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        var searchCategory = verifyContext.SearchCategories.Single();
        searchCategory.LastKnownImageCount.ShouldBe(0);
        searchCategory.LastPageVisited.ShouldBe(0);
    }

    [Fact]
    public async Task when_reset_database_async_executes_then_every_search_category_updated_at_is_set_to_the_current_time()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            var searchConfiguration = new SearchConfigurationEntity();
            searchConfiguration.SearchCategories.Add(new SearchCategoryEntity { Id = "nature-id", Name = "Nature", });
            context.ScrapeConfiguration.Add(new ScrapeConfigurationEntity { SearchConfiguration = searchConfiguration });
            context.SaveChanges();
        }

        var now = new DateTimeOffset(2026, 7, 21, 9, 30, 0, TimeSpan.Zero);
        var sut = CreateSut(() => now);

        await sut.ResetDatabaseAsync(TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.SearchCategories.Single().UpdatedAt.ShouldBe(now);
    }

    [Fact]
    public async Task when_remove_downloaded_files_async_executes_then_the_root_directory_is_deleted()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.ScrapeConfiguration.Add(new ScrapeConfigurationEntity { ScrapeDirectories = new ScrapeDirectoriesEntity { RootDirectory = "/wallpapers", } });
            context.SaveChanges();
        }

        fileSystem.Directory.CreateDirectory("/wallpapers/nature");
        fileSystem.File.WriteAllBytes("/wallpapers/nature/pic.jpg", [1, 2, 3]);
        var sut = CreateSut();

        await sut.RemoveDownloadedFilesAsync(TestContext.Current.CancellationToken);

        fileSystem.Directory.Exists("/wallpapers").ShouldBeFalse();
    }

    [Fact]
    public async Task when_remove_downloaded_files_async_executes_and_the_root_directory_does_not_exist_then_no_exception_is_thrown()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.ScrapeConfiguration.Add(new ScrapeConfigurationEntity { ScrapeDirectories = new ScrapeDirectoriesEntity { RootDirectory = "/wallpapers", } });
            context.SaveChanges();
        }

        var sut = CreateSut();

        await Should.NotThrowAsync(() => sut.RemoveDownloadedFilesAsync(TestContext.Current.CancellationToken));
    }

    public void Dispose() =>
        connection.Dispose();

    private DatabaseResetService CreateSut(Clock? clock = null) =>
        new(dbContextFactory, fileSystem, clock ?? (() => DateTimeOffset.UtcNow));

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
