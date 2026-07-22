using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenScrapeContextReader : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public GivenScrapeContextReader()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        dbContextFactory = new TestDbContextFactory(options);

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    [Fact]
    public async Task when_configuration_categories_ignore_lists_and_directories_all_exist_then_a_populated_context_is_returned()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            var searchConfiguration = new SearchConfigurationEntity { SearchStringPrefix = "https://wallhaven.cc/search?categories=", SearchStringSuffix = "&sorting=random", };
            context.SearchConfigurations.Add(searchConfiguration);
            context.SaveChanges();

            context.SearchCategories.Add(new SearchCategoryEntity { Id = "2", SearchConfigurationId = searchConfiguration.Id, Name = "Zebra", });
            context.SearchCategories.Add(new SearchCategoryEntity { Id = "1", SearchConfigurationId = searchConfiguration.Id, Name = "Anteater", });
            context.ModelsToIgnore.Add(new ModelToIgnoreEntity { Value = "Some Model", });
            context.TagsToIgnore.Add(new TagToIgnoreEntity { Value = "Some Tag", });
            context.ScrapeDirectories.Add(new ScrapeDirectoriesEntity { RootDirectory = "/root", BaseDirectory = "/base", BaseDirectoryFamous = "/famous", });
            context.SaveChanges();
        }

        var sut = new ScrapeContextReader(dbContextFactory);

        var result = await sut.ReadAsync(TestContext.Current.CancellationToken);

        result.Categories.ShouldBe([
            new ScrapeCategory("Anteater", "https://wallhaven.cc/search?categories=1&sorting=random", false, false),
            new ScrapeCategory("Zebra", "https://wallhaven.cc/search?categories=2&sorting=random", false, false),
        ]);
        result.ModelsToIgnore.ShouldBe(["Some Model"]);
        result.TagsToIgnore.ShouldBe(["Some Tag"]);
        result.Directories.ShouldBe(new DirectoryLayout("/root", "/base", "/famous"));
    }

    [Fact]
    public async Task when_a_category_is_marked_as_excluded_from_search_then_it_is_not_included_in_the_context()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            var searchConfiguration = new SearchConfigurationEntity { SearchStringPrefix = "https://wallhaven.cc/search?categories=", SearchStringSuffix = "&sorting=random", };
            context.SearchConfigurations.Add(searchConfiguration);
            context.SaveChanges();

            context.SearchCategories.Add(new SearchCategoryEntity { Id = "1", SearchConfigurationId = searchConfiguration.Id, Name = "Included", IncludeInSearch = true, });
            context.SearchCategories.Add(new SearchCategoryEntity { Id = "2", SearchConfigurationId = searchConfiguration.Id, Name = "Excluded", IncludeInSearch = false, });
            context.SaveChanges();
        }

        var sut = new ScrapeContextReader(dbContextFactory);

        var result = await sut.ReadAsync(TestContext.Current.CancellationToken);

        result.Categories.ShouldBe([new ScrapeCategory("Included", "https://wallhaven.cc/search?categories=1&sorting=random", false, false)]);
    }

    [Fact]
    public async Task when_categories_have_famous_and_internet_flags_then_famous_come_first_then_internet_then_the_rest_alphabetically()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            var searchConfiguration = new SearchConfigurationEntity { SearchStringPrefix = "https://wallhaven.cc/search?categories=", SearchStringSuffix = "&sorting=random", };
            context.SearchConfigurations.Add(searchConfiguration);
            context.SaveChanges();

            context.SearchCategories.Add(new SearchCategoryEntity { Id = "1", SearchConfigurationId = searchConfiguration.Id, Name = "Zebra", });
            context.SearchCategories.Add(new SearchCategoryEntity { Id = "2", SearchConfigurationId = searchConfiguration.Id, Name = "Anteater", });
            context.SearchCategories.Add(new SearchCategoryEntity { Id = "3", SearchConfigurationId = searchConfiguration.Id, Name = "Internet Model", IsInternet = true, });
            context.SearchCategories.Add(new SearchCategoryEntity { Id = "4", SearchConfigurationId = searchConfiguration.Id, Name = "Famous Person", IsFamous = true, });
            context.SaveChanges();
        }

        var sut = new ScrapeContextReader(dbContextFactory);

        var result = await sut.ReadAsync(TestContext.Current.CancellationToken);

        result.Categories.Select(category => category.Name).ShouldBe(["Famous Person", "Internet Model", "Anteater", "Zebra"]);
    }

    [Fact]
    public async Task when_no_search_configuration_exists_then_reading_throws()
    {
        var sut = new ScrapeContextReader(dbContextFactory);

        await Should.ThrowAsync<InvalidOperationException>(() => sut.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_no_scrape_directories_exist_then_the_directory_layout_defaults_to_empty_strings()
    {
        using (var context = dbContextFactory.CreateDbContext())
        {
            context.SearchConfigurations.Add(new SearchConfigurationEntity());
            context.SaveChanges();
        }

        var sut = new ScrapeContextReader(dbContextFactory);

        var result = await sut.ReadAsync(TestContext.Current.CancellationToken);

        result.Directories.ShouldBe(new DirectoryLayout(string.Empty, string.Empty, string.Empty));
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
