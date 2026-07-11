using NSubstitute.ExceptionExtensions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Pages;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using AStar.Dev.Wallpaper.Scraper.Support;
using AStar.Dev.Wallpaper.Scraper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scraper.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Serilog;
using AStar.Dev.Wallpaper.Scraper.Services;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Workflows;

public sealed class GivenATopWallpapersWorkflow : IAsyncLifetime
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

    private ImagePageService BuildImagePageService(IPlaywrightService playwrightService, ScrapeConfiguration scrapeConfiguration, IFileDetailRepository fileDetailRepository)
        => new(
            new ImagePage(playwrightService, scrapeConfiguration, new(), new()),
            fileDetailRepository,
            new FileClassificationService(BuildWorkingContextFactory()),
            scrapeConfiguration,
            System.TimeProvider.System,
            new LoggerConfiguration().CreateLogger(),
            Substitute.For<IDirectoryHelper>(),
            new(),
            new NoOpDelayStrategy(),
            Substitute.For<IImageRetriever>(),
            Substitute.For<IImageSaver>(),
            new MockFileSystem(),
            RepositoryTestDoubles.BuildScrapedTagRepository(),
            Substitute.For<IImageDimensionReader>());

    [Fact]
    public async Task when_run_against_a_working_page_with_zero_total_pages_then_the_result_is_a_success()
    {
        var topWallpapersPage = Substitute.For<ITopWallpapersPage>();
        topWallpapersPage.LoadTopWallpapersPageAsync(Arg.Any<int>()).Returns(Task.FromResult(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Value)));
        topWallpapersPage.PageInfoAsync().Returns(Task.FromResult(Result.Success<int, ScrapeError>(0)));
        topWallpapersPage.GetImagePageLinksAsync().Returns(Task.FromResult(Result.Success<IReadOnlyCollection<string>, ScrapeError>([])));

        var searchConfiguration = new SearchConfigurationBuilder { TopWallpapersStartingPageNumber = 1, TopWallpapersTotalPages = 0, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), Substitute.For<IDbContextFactory<AppDbContext>>());
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy());
        var imagePageService = BuildImagePageService(Substitute.For<IPlaywrightService>(), scrapeConfiguration, RepositoryTestDoubles.BuildFileDetailRepository());

        var sut = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        var result = await sut.RunAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_the_image_page_service_reports_a_failure_then_run_async_returns_a_failure_result()
    {
        var topWallpapersPage = Substitute.For<ITopWallpapersPage>();
        topWallpapersPage.LoadTopWallpapersPageAsync(Arg.Any<int>()).Returns(Task.FromResult(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Value)));
        topWallpapersPage.PageInfoAsync().Returns(Task.FromResult(Result.Success<int, ScrapeError>(1)));
        topWallpapersPage.GetImagePageLinksAsync().Returns(Task.FromResult(Result.Success<IReadOnlyCollection<string>, ScrapeError>(["https://example.test/w/12345",])));

        var searchConfiguration = new SearchConfigurationBuilder { TopWallpapersStartingPageNumber = 1, TopWallpapersTotalPages = 1, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var contextFactory = BuildWorkingContextFactory();
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy());

        var failingPlaywrightService = Substitute.For<IPlaywrightService>();
        failingPlaywrightService.ConfigurePlaywrightAsync().ThrowsAsync(new PlaywrightException("navigation failed"));
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository(exists: false);
        var imagePageService = BuildImagePageService(failingPlaywrightService, scrapeConfiguration, fileDetailRepository);

        var sut = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        var result = await sut.RunAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_run_then_the_delay_strategy_is_consulted_once_per_page_reported_by_the_header()
    {
        var topWallpapersPage = Substitute.For<ITopWallpapersPage>();
        topWallpapersPage.LoadTopWallpapersPageAsync(Arg.Any<int>()).Returns(Task.FromResult(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Value)));
        topWallpapersPage.PageInfoAsync().Returns(Task.FromResult(Result.Success<int, ScrapeError>(3)));
        topWallpapersPage.GetImagePageLinksAsync().Returns(Task.FromResult(Result.Success<IReadOnlyCollection<string>, ScrapeError>([])));

        var searchConfiguration = new SearchConfigurationBuilder { TopWallpapersStartingPageNumber = 1, TopWallpapersTotalPages = 1, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var contextFactory = BuildWorkingContextFactory();
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, delayStrategy);
        var imagePageService = BuildImagePageService(Substitute.For<IPlaywrightService>(), scrapeConfiguration, RepositoryTestDoubles.BuildFileDetailRepository());

        var sut = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        await sut.RunAsync(TestContext.Current.CancellationToken);

        await delayStrategy.Received(3).DelayAsync(DelayKind.PageNavigation, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_starting_page_fails_to_load_then_the_workflow_reloads_page_one()
    {
        var topWallpapersPage = Substitute.For<ITopWallpapersPage>();
        topWallpapersPage.LoadTopWallpapersPageAsync(5).Returns(Task.FromResult(Result.Failure<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(ScrapeErrorFactory.CreatePageLoadFailed("url", "boom"))));
        topWallpapersPage.LoadTopWallpapersPageAsync(1).Returns(Task.FromResult(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Value)));
        topWallpapersPage.PageInfoAsync().Returns(Task.FromResult(Result.Success<int, ScrapeError>(0)));
        topWallpapersPage.GetImagePageLinksAsync().Returns(Task.FromResult(Result.Success<IReadOnlyCollection<string>, ScrapeError>([])));

        var searchConfiguration = new SearchConfigurationBuilder { TopWallpapersStartingPageNumber = 5, TopWallpapersTotalPages = 0, }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), Substitute.For<IDbContextFactory<AppDbContext>>());
        var pagedScrapeRunner = new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy());
        var imagePageService = BuildImagePageService(Substitute.For<IPlaywrightService>(), scrapeConfiguration, RepositoryTestDoubles.BuildFileDetailRepository());

        var sut = new TopWallpapersWorkflow(topWallpapersPage, imagePageService, scrapeConfiguration, configurationSaver, pagedScrapeRunner, new LoggerConfiguration().CreateLogger());

        await sut.RunAsync(TestContext.Current.CancellationToken);

        await topWallpapersPage.Received(1).LoadTopWallpapersPageAsync(1);
    }
}
