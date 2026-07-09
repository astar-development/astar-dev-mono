using NSubstitute.ExceptionExtensions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenAConfigurationSaver : IAsyncLifetime
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
            ConnectionStrings = new ConnectionStringsEntity { Sqlite = "Data Source=test.db", },
            UserConfiguration = new UserConfigurationEntity { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie", },
            SearchConfiguration = new SearchConfigurationEntity { BaseUrl = new Uri("https://example.com"), },
            ScrapeDirectories = new ScrapeDirectoriesEntity { RootDirectory = "root-directory", },
        });
        await seedContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_saving_completes_successfully_then_the_result_is_ok()
    {
        var category = new Category { Id = "cat-1", Name = "Cars", LastKnownImageCount = 10, LastPageVisited = 2, TotalPages = 5, };
        var searchConfiguration = new SearchConfigurationBuilder { SearchCategories = [category,], }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var sut = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);

        var result = await sut.SaveUpdatedConfigurationAsync();

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_saving_a_new_category_then_it_is_persisted()
    {
        var category = new Category { Id = "cat-new", Name = "Nature", LastKnownImageCount = 3, LastPageVisited = 1, TotalPages = 2, };
        var searchConfiguration = new SearchConfigurationBuilder { SearchCategories = [category,], }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var sut = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);

        await sut.SaveUpdatedConfigurationAsync();

        await using var verifyContext = new AppDbContext(options);
        var saved = await verifyContext.SearchConfigurations.SelectMany(sc => sc.SearchCategories).SingleAsync(c => c.Id == "cat-new", TestContext.Current.CancellationToken);
        saved.Name.ShouldBe("Nature");
    }

    [Fact]
    public async Task when_the_context_factory_throws_then_a_configuration_save_failed_error_is_returned()
    {
        var scrapeConfiguration = new ScrapeConfigurationBuilder().Build();
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("db unavailable"));
        var sut = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);

        var result = await sut.SaveUpdatedConfigurationAsync();

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<ConfigurationSaveFailed>();
    }

    [Fact]
    public async Task when_the_context_factory_throws_then_no_exception_is_thrown()
    {
        var scrapeConfiguration = new ScrapeConfigurationBuilder().Build();
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("db unavailable"));
        var sut = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);

        var exception = await Record.ExceptionAsync(() => sut.SaveUpdatedConfigurationAsync());

        exception.ShouldBeNull();
    }
}
