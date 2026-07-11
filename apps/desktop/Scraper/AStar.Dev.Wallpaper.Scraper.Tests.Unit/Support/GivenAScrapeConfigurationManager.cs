using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Support;

public sealed class GivenAScrapeConfigurationManager : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();

        seedContext.ScrapeConfiguration.Add(new ScrapeConfigurationEntity
        {
            ConnectionStrings = new ConnectionStringsEntity { Sqlite = "Data Source=test.db" },
            UserConfiguration = new UserConfigurationEntity { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie" },
            SearchConfiguration = new SearchConfigurationEntity { BaseUrl = new Uri("https://example.com"), SearchString = "original" },
            ScrapeDirectories = new ScrapeDirectoriesEntity { RootDirectory = "root-directory" },
        });
        await seedContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public void when_constructed_then_current_reflects_the_persisted_configuration()
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContext().Returns(_ => new AppDbContext(options));

        var sut = new ScrapeConfigurationManager(contextFactory);

        sut.Current.SearchConfiguration.SearchString.ShouldBe("original");
    }

    [Fact]
    public void when_constructed_then_only_one_db_context_is_created()
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContext().Returns(_ => new AppDbContext(options));

        _ = new ScrapeConfigurationManager(contextFactory);

        contextFactory.Received(1).CreateDbContext();
    }

    [Fact]
    public async Task when_reloaded_after_an_external_change_then_current_reflects_the_new_value()
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContext().Returns(_ => new AppDbContext(options));
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var sut = new ScrapeConfigurationManager(contextFactory);

        await using (var writeContext = new AppDbContext(options))
        {
            var entity = await writeContext.ScrapeConfiguration.Include(e => e.SearchConfiguration).SingleAsync(TestContext.Current.CancellationToken);
            entity.SearchConfiguration.SearchString = "updated";
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await sut.ReloadAsync(TestContext.Current.CancellationToken);

        sut.Current.SearchConfiguration.SearchString.ShouldBe("updated");
    }
}
