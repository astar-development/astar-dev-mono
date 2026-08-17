using AStar.Dev.Infrastructure.AppDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Data.Entities;

public sealed class GivenAScrapeConfigurationEntity : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly AppDbContext context;
    private bool disposed;

    public GivenAScrapeConfigurationEntity()
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

    private static ScrapeConfigurationEntity CreateScrapeConfiguration() =>
        new()
        {
            ConnectionStrings = new ConnectionStringsEntity { Sqlite = "Data Source=files.db" },
            UserConfiguration = new UserConfigurationEntity { Username = "jay", LoginEmailAddress = "jay@example.com" },
            SearchConfiguration = new SearchConfigurationEntity
            {
                BaseUrl = new Uri("https://example.com"),
                LoginUrl = new Uri("https://example.com/login"),
                SearchCategories = { new SearchCategoryEntity { Id = "nature", Name = "Nature" } }
            },
            ScrapeDirectories = new ScrapeDirectoriesEntity { RootDirectory = "/scraped" }
        };

    [Fact]
    public async Task when_a_scrape_configuration_is_added_then_its_connection_strings_are_persisted()
    {
        context.ScrapeConfiguration.Add(CreateScrapeConfiguration());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.ScrapeConfiguration.Include(config => config.ConnectionStrings).First();

        retrieved.ConnectionStrings.Sqlite.ShouldBe("Data Source=files.db");
    }

    [Fact]
    public async Task when_a_scrape_configuration_is_added_then_its_search_categories_are_persisted()
    {
        context.ScrapeConfiguration.Add(CreateScrapeConfiguration());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var retrieved = context.ScrapeConfiguration
                                .Include(config => config.SearchConfiguration)
                                .ThenInclude(searchConfiguration => searchConfiguration.SearchCategories)
                                .First();

        retrieved.SearchConfiguration.SearchCategories.ShouldHaveSingleItem();
        retrieved.SearchConfiguration.SearchCategories.First().Name.ShouldBe("Nature");
    }

    [Fact]
    public async Task when_a_scrape_configuration_is_removed_then_its_scrape_directories_are_cascade_deleted()
    {
        var configuration = CreateScrapeConfiguration();
        context.ScrapeConfiguration.Add(configuration);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.ScrapeConfiguration.Remove(configuration);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Set<ScrapeDirectoriesEntity>().Any().ShouldBeFalse();
    }
}
