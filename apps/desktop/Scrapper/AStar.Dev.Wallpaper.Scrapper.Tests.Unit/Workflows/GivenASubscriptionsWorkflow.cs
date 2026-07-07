using NSubstitute.ExceptionExtensions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scrapper.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Serilog;
using AStar.Dev.Wallpaper.Scrapper.Services;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Workflows;

public sealed class GivenASubscriptionsWorkflow : IAsyncLifetime
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

    private IDbContextFactory<AppDbContext> BuildWorkingContextFactory()
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        return contextFactory;
    }

    private static (IPage Page, IPlaywrightService PlaywrightService, ILocator ClearAllLocator) BuildPlaywright(string headerText, IReadOnlyList<ILocator> imageLinks)
    {
        var response = Substitute.For<IResponse>();
        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>()).Returns(Task.FromResult<IResponse?>(response));

        var headerLocator = Substitute.For<ILocator>();
        headerLocator.TextContentAsync().Returns(Task.FromResult<string?>(headerText));
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions>()).Returns(headerLocator);

        var linksLocator = Substitute.For<ILocator>();
        linksLocator.AllAsync().Returns(Task.FromResult(imageLinks));
        page.GetByRole(AriaRole.Link).Returns(linksLocator);

        var divLocator = Substitute.For<ILocator>();
        page.Locator("div").Returns(divLocator);
        var filteredLocator = Substitute.For<ILocator>();
        divLocator.Filter(Arg.Any<LocatorFilterOptions>()).Returns(filteredLocator);
        var clearAllLocator = Substitute.For<ILocator>();
        filteredLocator.GetByRole(AriaRole.Link, Arg.Any<LocatorGetByRoleOptions>()).Returns(clearAllLocator);
        clearAllLocator.ClickAsync().Returns(Task.CompletedTask);

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        return (page, playwrightService, clearAllLocator);
    }

    private ImagePageService BuildImagePageService(IPlaywrightService playwrightService, ScrapeConfiguration scrapeConfiguration, IDirectoryHelper directoryHelper, IImageRetriever imageRetriever)
        => new(
            new ImagePage(playwrightService, scrapeConfiguration, new(), new()),
            Substitute.For<IFileDetailRepository>(),
            new FileClassificationService(BuildWorkingContextFactory(),new LoggerConfiguration().CreateLogger()),
            scrapeConfiguration,
            System.TimeProvider.System,
            new LoggerConfiguration().CreateLogger(),
            directoryHelper,
            new(),
            new NoOpDelayStrategy(),
            imageRetriever,
            Substitute.For<IImageSaver>(),
            new MockFileSystem(),
            Substitute.For<IScrapedTagRepository>(),
            Substitute.For<IImageDimensionReader>());

    [Fact]
    public async Task when_run_against_a_working_page_with_zero_total_pages_then_the_result_is_a_success()
    {
        var (_, playwrightService, _) = BuildPlaywright("0 New Subscription Wallpapers", []);
        var searchConfiguration = new SearchConfigurationBuilder { SubscriptionsStartingPageNumber = 1, SubscriptionsTotalPages = 0, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var subscriptionsImagesListPage = new SubscriptionsImagesListPage(playwrightService, searchConfiguration);
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), Substitute.For<IDbContextFactory<AppDbContext>>());
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy());
        var imagePageService = BuildImagePageService(playwrightService, scrapeConfiguration, Substitute.For<IDirectoryHelper>(), Substitute.For<IImageRetriever>());

        var sut = new SubscriptionsWorkflow(subscriptionsImagesListPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        var result = await sut.RunAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_the_image_page_service_reports_a_failure_then_run_async_returns_a_failure_result()
    {
        var linkLocator = Substitute.For<ILocator>();
        linkLocator.GetAttributeAsync("href").Returns(Task.FromResult<string?>("https://example.test/w/12345"));
        var (_, playwrightService, _) = BuildPlaywright("1 New Subscription Wallpapers", [linkLocator,]);

        var searchConfiguration = new SearchConfigurationBuilder { SubscriptionsStartingPageNumber = 1, SubscriptionsTotalPages = 1, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var subscriptionsImagesListPage = new SubscriptionsImagesListPage(playwrightService, searchConfiguration);

        var contextFactory = BuildWorkingContextFactory();
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy());

        var failingPlaywrightService = Substitute.For<IPlaywrightService>();
        failingPlaywrightService.ConfigurePlaywrightAsync().ThrowsAsync(new PlaywrightException("navigation failed"));
        var imagePageService = BuildImagePageService(failingPlaywrightService, scrapeConfiguration, Substitute.For<IDirectoryHelper>(), Substitute.For<IImageRetriever>());

        var sut = new SubscriptionsWorkflow(subscriptionsImagesListPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        var result = await sut.RunAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_the_page_count_is_greater_than_zero_then_the_clear_all_locator_is_clicked_after_the_loop()
    {
        var (_, playwrightService, clearAllLocator) = BuildPlaywright("1 New Subscription Wallpapers", []);
        var searchConfiguration = new SearchConfigurationBuilder { SubscriptionsStartingPageNumber = 1, SubscriptionsTotalPages = 1, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var subscriptionsImagesListPage = new SubscriptionsImagesListPage(playwrightService, searchConfiguration);

        var contextFactory = BuildWorkingContextFactory();
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy());
        var imagePageService = BuildImagePageService(playwrightService, scrapeConfiguration, Substitute.For<IDirectoryHelper>(), Substitute.For<IImageRetriever>());

        var sut = new SubscriptionsWorkflow(subscriptionsImagesListPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        await sut.RunAsync(TestContext.Current.CancellationToken);

        await clearAllLocator.Received(1).ClickAsync();
    }

    [Fact]
    public async Task when_the_page_count_is_zero_then_the_clear_all_locator_is_not_clicked()
    {
        var (_, playwrightService, clearAllLocator) = BuildPlaywright("0 New Subscription Wallpapers", []);
        var searchConfiguration = new SearchConfigurationBuilder { SubscriptionsStartingPageNumber = 1, SubscriptionsTotalPages = 0, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var subscriptionsImagesListPage = new SubscriptionsImagesListPage(playwrightService, searchConfiguration);
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), Substitute.For<IDbContextFactory<AppDbContext>>());
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy());
        var imagePageService = BuildImagePageService(playwrightService, scrapeConfiguration, Substitute.For<IDirectoryHelper>(), Substitute.For<IImageRetriever>());

        var sut = new SubscriptionsWorkflow(subscriptionsImagesListPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        await sut.RunAsync(TestContext.Current.CancellationToken);

        await clearAllLocator.DidNotReceive().ClickAsync();
    }

    [Fact]
    public async Task when_the_header_reports_a_sub_directory_name_then_the_processed_links_use_it_as_the_category_name()
    {
        const string ExpectedSubDirectoryFragment = "New-Subscription-Wallpapers";
        var linkLocator = Substitute.For<ILocator>();
        linkLocator.GetAttributeAsync("href").Returns(Task.FromResult<string?>("https://example.test/w/12345"));
        var (_, playwrightService, _) = BuildPlaywright("1 New Subscription Wallpapers", [linkLocator,]);

        var searchConfiguration = new SearchConfigurationBuilder { SubscriptionsStartingPageNumber = 1, SubscriptionsTotalPages = 1, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var subscriptionsImagesListPage = new SubscriptionsImagesListPage(playwrightService, searchConfiguration);

        var contextFactory = BuildWorkingContextFactory();
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy());

        var directoryHelper = Substitute.For<IDirectoryHelper>();
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<byte[], ScrapeError>(ScrapeErrorFactory.CreateImageDownloadFailed("url", "boom"))));
        var imagePageService = BuildImagePageService(playwrightService, scrapeConfiguration, directoryHelper, imageRetriever);

        var sut = new SubscriptionsWorkflow(subscriptionsImagesListPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        await sut.RunAsync(TestContext.Current.CancellationToken);

        directoryHelper.Received(1).CreateDirectoryIfRequired(Arg.Is<List<string>>(segments => segments.Any(segment => segment.Contains(ExpectedSubDirectoryFragment))));
    }

    [Fact]
    public async Task when_run_then_the_delay_strategy_is_consulted_once_per_page()
    {
        var (_, playwrightService, _) = BuildPlaywright("25 New Subscription Wallpapers", []);
        var searchConfiguration = new SearchConfigurationBuilder { SubscriptionsStartingPageNumber = 1, SubscriptionsTotalPages = 2, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var subscriptionsImagesListPage = new SubscriptionsImagesListPage(playwrightService, searchConfiguration);

        var contextFactory = BuildWorkingContextFactory();
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, delayStrategy);
        var imagePageService = BuildImagePageService(playwrightService, scrapeConfiguration, Substitute.For<IDirectoryHelper>(), Substitute.For<IImageRetriever>());

        var sut = new SubscriptionsWorkflow(subscriptionsImagesListPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        await sut.RunAsync(TestContext.Current.CancellationToken);

        await delayStrategy.Received(2).DelayAsync(DelayKind.PageNavigation, Arg.Any<CancellationToken>());
    }
}
