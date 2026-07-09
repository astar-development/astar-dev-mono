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

public sealed class GivenASearchWorkflowThatFails : IAsyncLifetime
{
    private const string SearchStringPrefix = "https://example.test/search/";
    private const string SearchStringSuffix = "/?page=";
    private const string CategoryId = "cat-fails";

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

    private SearchWorkflow BuildSut(IPage page, ILogger logger)
    {
        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var category = new Category { Id = CategoryId, Name = "Fails Category", LastKnownImageCount = 999, TotalPages = 999, };
        var searchConfiguration = new SearchConfigurationBuilder
        {
            SearchCategories = [category,],
            SearchString = $"{SearchStringPrefix}{category.Id}{SearchStringSuffix}",
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
        var imagePageService = new ImagePageService(imagePage, Substitute.For<IFileDetailRepository>(), fileClassificationService, scrapeConfiguration, System.TimeProvider.System, new LoggerConfiguration().CreateLogger(), Substitute.For<IDirectoryHelper>(), new(), new NoOpDelayStrategy(), Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IScrapedTagRepository>(), Substitute.For<IImageDimensionReader>());

        return new SearchWorkflow(searchResultsPage, scrapeConfiguration, configurationSaver, imagePageService, Substitute.For<IDirectoryHelper>(), logger, new NoOpDelayStrategy(), System.TimeProvider.System, new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy()));
    }

    [Fact]
    public async Task when_the_search_results_page_fails_to_load_then_run_async_returns_a_failure()
    {
        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).ThrowsAsync(new PlaywrightException("navigation failed"));
        var sut = BuildSut(page, Substitute.For<ILogger>());

        var result = await sut.RunAsync(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<PageLoadFailed>();
    }

    [Fact]
    public async Task when_the_search_results_page_fails_to_load_then_the_logger_receives_exactly_one_error_call()
    {
        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).ThrowsAsync(new PlaywrightException("navigation failed"));
        var logger = Substitute.For<ILogger>();
        var sut = BuildSut(page, logger);

        await sut.RunAsync(TestContext.Current.CancellationToken);

        logger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }
}
