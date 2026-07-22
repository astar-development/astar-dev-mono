using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenSearchCategoryReader : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public GivenSearchCategoryReader()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_a_matching_category_exists_then_its_stored_progress_is_returned()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            var searchConfiguration = new SearchConfigurationEntity();
            searchConfiguration.SearchCategories.Add(new SearchCategoryEntity
            {
                Id = "nature-id",
                Name = "Nature",
                IsFamous = false,
                IsInternet = false,
                TotalPages = 5,
                LastKnownImageCount = 120,
                LastPageVisited = 3
            });
            context.SearchConfigurations.Add(searchConfiguration);
            context.SaveChanges();
        }

        var sut = new SearchCategoryReader(dbContextFactory);

        var progress = await sut.GetProgressAsync("Nature", TestContext.Current.CancellationToken);

        progress.ShouldBe(new Option<SearchCategoryProgress>.Some(new SearchCategoryProgress(120, 3)));
    }

    [Fact]
    public async Task when_no_matching_category_exists_then_none_is_returned()
    {
        var sut = new SearchCategoryReader(dbContextFactory);

        var progress = await sut.GetProgressAsync("Missing", TestContext.Current.CancellationToken);

        progress.ShouldBe(Option<SearchCategoryProgress>.None.Instance);
    }

    public void Dispose() =>
        connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
