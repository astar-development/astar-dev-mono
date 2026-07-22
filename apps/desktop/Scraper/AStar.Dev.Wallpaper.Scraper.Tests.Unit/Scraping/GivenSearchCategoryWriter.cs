using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenSearchCategoryWriter : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public GivenSearchCategoryWriter()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_a_matching_category_exists_then_its_scrape_progress_and_flags_are_updated()
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
                TotalPages = 0,
                LastKnownImageCount = 0,
                LastPageVisited = 0
            });
            context.SearchConfigurations.Add(searchConfiguration);
            context.SaveChanges();
        }

        var sut = new SearchCategoryWriter(dbContextFactory);
        SearchCategoryDto searchCategory = new("Nature", true, true, 5, 120, 3);

        var result = await sut.WriteAsync(searchCategory, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<FunctionalParadigm.Unit, string>>();
        using var verifyContext = dbContextFactory.CreateDbContext();
        var updated = verifyContext.SearchCategories.Single(category => category.Name == "Nature");
        updated.IsFamous.ShouldBeTrue();
        updated.IsInternet.ShouldBeTrue();
        updated.TotalPages.ShouldBe(5);
        updated.LastKnownImageCount.ShouldBe(120);
        updated.LastPageVisited.ShouldBe(3);
    }

    [Fact]
    public async Task when_no_matching_category_exists_then_a_failure_result_is_returned_and_nothing_is_written()
    {
        var sut = new SearchCategoryWriter(dbContextFactory);
        SearchCategoryDto searchCategory = new("Missing", false, false, 1, 1, 1);

        var result = await sut.WriteAsync(searchCategory, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<FunctionalParadigm.Unit, string>>();
        using var verifyContext = dbContextFactory.CreateDbContext();
        verifyContext.SearchCategories.Count().ShouldBe(0);
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
