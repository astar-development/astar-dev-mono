using System.Diagnostics;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Workflows;

public sealed class GivenASearchWorkflowWithADelayStrategy : IAsyncLifetime
{
    private const string SearchStringPrefix = "https://example.test/search/";
    private const string SearchStringSuffix = "/?page=";
    private const string CategoryId = "cat-delay";

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

    private static (IPage Page, IPlaywrightService PlaywrightService) BuildPlaywright(string headerText, bool includeLinks)
    {
        var okResponse = Substitute.For<IResponse>();
        okResponse.Ok.Returns(true);

        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns(Task.FromResult<IResponse?>(okResponse));

        var headerLocator = Substitute.For<ILocator>();
        headerLocator.TextContentAsync().Returns(Task.FromResult<string?>(headerText));
        page.GetByText(Arg.Is<string>(text => text.Contains("Wallpapers found")), Arg.Any<PageGetByTextOptions>()).Returns(headerLocator);

        var linksLocator = Substitute.For<ILocator>();
        linksLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>(includeLinks ? [] : []));
        page.GetByRole(AriaRole.Link, Arg.Any<PageGetByRoleOptions>()).Returns(linksLocator);

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        return (page, playwrightService);
    }

    [Fact]
    public async Task when_a_category_is_up_to_date_then_the_delay_strategy_receives_the_category_up_to_date_delay()
    {
        var category = new Category { Id = CategoryId, Name = "Delay Category", LastKnownImageCount = 10, TotalPages = 1, };
        string matchingSearchString = $"{SearchStringPrefix}{category.Id}{SearchStringSuffix}";
        var (_, playwrightService) = BuildPlaywright("10 Wallpapers found for #Delay", includeLinks: false);

        var searchConfiguration = new SearchConfigurationBuilder
        {
            SearchCategories = [category,],
            SearchString = matchingSearchString,
            SearchStringPrefix = SearchStringPrefix,
            SearchStringSuffix = SearchStringSuffix,
        }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();

        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        var searchResultsPage = new SearchResultsPage(playwrightService);
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var imagePage = new ImagePage(playwrightService, scrapeConfiguration, new(), new());
        var fileClassificationService = new FileClassificationService(contextFactory,new LoggerConfiguration().CreateLogger());
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePageService = new ImagePageService(imagePage, Substitute.For<IFileDetailRepository>(), fileClassificationService, scrapeConfiguration, System.TimeProvider.System, new LoggerConfiguration().CreateLogger(), Substitute.For<IDirectoryHelper>(), new(), delayStrategy, Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IScrapedTagRepository>(), Substitute.For<IImageDimensionReader>());

        var sut = new SearchWorkflow(searchResultsPage, scrapeConfiguration, configurationSaver, imagePageService, Substitute.For<IDirectoryHelper>(), Substitute.For<ILogger>(), delayStrategy, System.TimeProvider.System, new PagedScrapeRunner(configurationSaver, delayStrategy));

        await sut.RunAsync(TestContext.Current.CancellationToken);

        await delayStrategy.Received(1).DelayAsync(DelayKind.CategoryUpToDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_category_is_not_up_to_date_then_the_delay_strategy_receives_a_page_navigation_delay_for_each_page_visited()
    {
        var category = new Category { Id = CategoryId, Name = "Delay Category", LastKnownImageCount = 999, TotalPages = 999, };
        string matchingSearchString = $"{SearchStringPrefix}{category.Id}{SearchStringSuffix}";
        var (_, playwrightService) = BuildPlaywright("48 Wallpapers found for #Delay", includeLinks: false);

        var searchConfiguration = new SearchConfigurationBuilder
        {
            SearchCategories = [category,],
            SearchString = matchingSearchString,
            SearchStringPrefix = SearchStringPrefix,
            SearchStringSuffix = SearchStringSuffix,
        }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();

        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        var searchResultsPage = new SearchResultsPage(playwrightService);
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var imagePage = new ImagePage(playwrightService, scrapeConfiguration, new(), new());
        var fileClassificationService = new FileClassificationService(contextFactory, new LoggerConfiguration().CreateLogger());
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePageService = new ImagePageService(imagePage, Substitute.For<IFileDetailRepository>(), fileClassificationService, scrapeConfiguration, System.TimeProvider.System, new LoggerConfiguration().CreateLogger(), Substitute.For<IDirectoryHelper>(), new(), delayStrategy, Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IScrapedTagRepository>(), Substitute.For<IImageDimensionReader>());

        var sut = new SearchWorkflow(searchResultsPage, scrapeConfiguration, configurationSaver, imagePageService, Substitute.For<IDirectoryHelper>(), Substitute.For<ILogger>(), delayStrategy, System.TimeProvider.System, new PagedScrapeRunner(configurationSaver, delayStrategy));

        await sut.RunAsync(TestContext.Current.CancellationToken);

        await delayStrategy.Received(2).DelayAsync(DelayKind.PageNavigation, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_delay_strategy_is_a_zero_delay_substitute_then_run_async_completes_almost_instantly()
    {
        var category = new Category { Id = CategoryId, Name = "Delay Category", LastKnownImageCount = 999, TotalPages = 999, };
        string matchingSearchString = $"{SearchStringPrefix}{category.Id}{SearchStringSuffix}";
        var (_, playwrightService) = BuildPlaywright("48 Wallpapers found for #Delay", includeLinks: false);

        var searchConfiguration = new SearchConfigurationBuilder
        {
            SearchCategories = [category,],
            SearchString = matchingSearchString,
            SearchStringPrefix = SearchStringPrefix,
            SearchStringSuffix = SearchStringSuffix,
        }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();

        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        var searchResultsPage = new SearchResultsPage(playwrightService);
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var imagePage = new ImagePage(playwrightService, scrapeConfiguration, new(), new());
        var fileClassificationService = new FileClassificationService(contextFactory, new LoggerConfiguration().CreateLogger());
        var delayStrategy = new NoOpDelayStrategy();
        var imagePageService = new ImagePageService(imagePage, Substitute.For<IFileDetailRepository>(), fileClassificationService, scrapeConfiguration, System.TimeProvider.System, new LoggerConfiguration().CreateLogger(), Substitute.For<IDirectoryHelper>(), new(), delayStrategy, Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IScrapedTagRepository>(), Substitute.For<IImageDimensionReader>());

        var sut = new SearchWorkflow(searchResultsPage, scrapeConfiguration, configurationSaver, imagePageService, Substitute.For<IDirectoryHelper>(), Substitute.For<ILogger>(), delayStrategy, System.TimeProvider.System, new PagedScrapeRunner(configurationSaver, delayStrategy));

        var stopwatch = Stopwatch.StartNew();
        await sut.RunAsync(TestContext.Current.CancellationToken);
        stopwatch.Stop();

        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
    }
}
