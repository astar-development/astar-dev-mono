using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AStar.Dev.Wallpaper.Scraper.Scraping.SearchCategories;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping.SearchCategories;

public sealed class GivenSearchConfigurationProgressWriter : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;
    private bool disposed;

    public GivenSearchConfigurationProgressWriter()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
        context.ScrapeConfiguration.Add(new ScrapeConfigurationEntity { SearchConfiguration = new SearchConfigurationEntity(), });
        context.SaveChanges();
    }

    [Fact]
    public async Task when_writing_top_wallpapers_progress_then_the_starting_page_and_total_pages_are_persisted()
    {
        var sut = new SearchConfigurationProgressWriter(dbContextFactory);

        await sut.WriteTopWallpapersProgressAsync(4, 42, TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        var updated = verifyContext.SearchConfigurations.Single();
        updated.TopWallpapersStartingPageNumber.ShouldBe(4);
        updated.TopWallpapersTotalPages.ShouldBe(42);
    }

    [Fact]
    public async Task when_writing_subscriptions_progress_then_the_starting_page_and_total_pages_are_persisted()
    {
        var sut = new SearchConfigurationProgressWriter(dbContextFactory);

        await sut.WriteSubscriptionsProgressAsync(2, 9, TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        var updated = verifyContext.SearchConfigurations.Single();
        updated.SubscriptionsStartingPageNumber.ShouldBe(2);
        updated.SubscriptionsTotalPages.ShouldBe(9);
    }

    [Fact]
    public async Task when_writing_top_wallpapers_progress_then_subscriptions_progress_is_left_unchanged()
    {
        var sut = new SearchConfigurationProgressWriter(dbContextFactory);
        await sut.WriteSubscriptionsProgressAsync(2, 9, TestContext.Current.CancellationToken);

        await sut.WriteTopWallpapersProgressAsync(4, 42, TestContext.Current.CancellationToken);

        using var verifyContext = dbContextFactory.CreateDbContext();
        var updated = verifyContext.SearchConfigurations.Single();
        updated.SubscriptionsStartingPageNumber.ShouldBe(2);
        updated.SubscriptionsTotalPages.ShouldBe(9);
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
