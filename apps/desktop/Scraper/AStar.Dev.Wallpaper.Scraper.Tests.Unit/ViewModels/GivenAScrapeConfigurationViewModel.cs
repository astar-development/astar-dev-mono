using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scraper.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.ViewModels;

public sealed class GivenAScrapeConfigurationViewModel : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;
    private IDbContextFactory<AppDbContext> contextFactory = null!;
    private ScrapeConfigurationManager scrapeConfigurationManager = null!;

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
            UserConfiguration = new UserConfigurationEntity
            {
                LoginEmailAddress = "test@example.com",
                Username = "testuser",
                Password = "password",
                SessionCookie = "cookie"
            },
            SearchConfiguration = new SearchConfigurationEntity
            {
                BaseUrl = new Uri("https://example.com"),
                ApiKey = "key",
                LoginUrl = new Uri("https://example.com/login"),
                SearchString = "search",
                SearchStringPrefix = "prefix",
                SearchStringSuffix = "suffix",
                TopWallpapers = "top",
                Subscriptions = "subs",
                ImagePauseInSeconds = 1,
                StartingPageNumber = 1,
                TotalPages = 10,
                UseHeadless = true,
                SlowMotionDelay = 0.5f,
                SubscriptionsStartingPageNumber = 1,
                SubscriptionsTotalPages = 5,
                TopWallpapersTotalPages = 20,
                TopWallpapersStartingPageNumber = 1,
            },
            ScrapeDirectories = new ScrapeDirectoriesEntity
            {
                RootDirectory = "/root",
                BaseSaveDirectory = "/save",
                BaseDirectory = "/base",
                BaseDirectoryFamous = "/famous",
                SubDirectoryName = "sub"
            }
        });
        await seedContext.SaveChangesAsync();
        var config = await seedContext.SearchConfigurations.SingleAsync();
        var category = new SearchCategoryEntity
        {
            SearchConfigurationId = config.Id,
            Id = "cat1",
            Name = "Category 1",
            LastKnownImageCount = 10,
            LastPageVisited = 1,
            TotalPages = 5,
            IncludeInSearch = true
        };
        seedContext.Add(category);
        await seedContext.SaveChangesAsync();

        contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new AppDbContext(options)));
        contextFactory.CreateDbContext().Returns(new AppDbContext(options));

        scrapeConfigurationManager = Substitute.ForPartsOf<ScrapeConfigurationManager>(contextFactory);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_load_async_is_called_then_it_uses_per_operation_context()
    {
        var sut = new ScrapeConfigurationViewModel(contextFactory, scrapeConfigurationManager);

        var result = await sut.LoadAsync(CancellationToken.None);

        await contextFactory.Received(1).CreateDbContextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_load_async_is_called_with_cancellation_token_then_token_is_propagated()
    {
        using var cts = new CancellationTokenSource();
        var sut = new ScrapeConfigurationViewModel(contextFactory, scrapeConfigurationManager);

        var result = await sut.LoadAsync(cts.Token);

        await contextFactory.Received(1).CreateDbContextAsync(cts.Token);
    }

    [Fact]
    public async Task when_load_async_succeeds_then_returns_ok_result()
    {
        var sut = new ScrapeConfigurationViewModel(contextFactory, scrapeConfigurationManager);

        var result = await sut.LoadAsync(CancellationToken.None);

        result.ShouldBeOfType<Ok<FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_load_async_succeeds_then_properties_are_populated()
    {
        var sut = new ScrapeConfigurationViewModel(contextFactory, scrapeConfigurationManager);

        await sut.LoadAsync(CancellationToken.None);

        sut.Sqlite.ShouldBe("Data Source=test.db");
        sut.LoginEmailAddress.ShouldBe("test@example.com");
        sut.BaseUrl.ShouldBe("https://example.com/");
    }

    [Fact]
    public async Task when_save_async_is_called_then_it_uses_per_operation_context()
    {
        var sut = new ScrapeConfigurationViewModel(contextFactory, scrapeConfigurationManager);
        await sut.LoadAsync(CancellationToken.None);

        var result = await sut.SaveAsync(CancellationToken.None);

        // LoadAsync calls CreateDbContextAsync once, SaveAsync calls it once, and SaveAsync -> ReloadAsync calls it once = 3 total
        await contextFactory.Received(3).CreateDbContextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_save_async_is_called_with_cancellation_token_then_token_is_propagated()
    {
        using var cts = new CancellationTokenSource();
        var sut = new ScrapeConfigurationViewModel(contextFactory, scrapeConfigurationManager);
        await sut.LoadAsync(CancellationToken.None);

        var result = await sut.SaveAsync(cts.Token);

        await contextFactory.Received().CreateDbContextAsync(cts.Token);
    }

    [Fact]
    public async Task when_save_async_succeeds_then_returns_ok_result()
    {
        var sut = new ScrapeConfigurationViewModel(contextFactory, scrapeConfigurationManager);
        await sut.LoadAsync(CancellationToken.None);

        var result = await sut.SaveAsync(CancellationToken.None);

        result.ShouldBeOfType<Ok<FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_save_async_succeeds_then_reload_manager_is_called()
    {
        var sut = new ScrapeConfigurationViewModel(contextFactory, scrapeConfigurationManager);
        await sut.LoadAsync(CancellationToken.None);

        await sut.SaveAsync(CancellationToken.None);

        await scrapeConfigurationManager.Received(1).ReloadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_load_async_fails_then_returns_error_result()
    {
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns<Task<AppDbContext>>(_ => throw new InvalidOperationException("Test exception"));
        var sut = new ScrapeConfigurationViewModel(contextFactory, scrapeConfigurationManager);

        var result = await sut.LoadAsync(CancellationToken.None);

        result.ShouldBeOfType<Fail<FunctionalParadigm.Unit, ScrapeError>>();
    }
}
