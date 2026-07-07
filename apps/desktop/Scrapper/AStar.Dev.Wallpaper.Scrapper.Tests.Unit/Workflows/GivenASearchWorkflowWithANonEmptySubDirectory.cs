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

public sealed class GivenASearchWorkflowWithANonEmptySubDirectory : IAsyncLifetime
{
    private const string CategoryId = "cat-4";
    private const string RootDirectory = "root-directory";
    private const string BaseDirectory = "base-directory";
    private const string HeaderText = "24 Wallpapers found for #TestSubDir";
    private const string ExpectedSubDirectoryName = "TestSubDir";

    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();

        seedContext.ScrapeConfiguration.Add(new ScrapeConfigurationEntity
        {
            ConnectionStrings = new ConnectionStringsEntity { Sqlite = "Data Source=test.db", },
            UserConfiguration = new UserConfigurationEntity { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie", },
            SearchConfiguration = new SearchConfigurationEntity { BaseUrl = new Uri("https://example.com"), },
            ScrapeDirectories = new ScrapeDirectoriesEntity { RootDirectory = RootDirectory, },
        });
        await seedContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_the_page_reports_a_non_empty_sub_directory_name_then_the_directory_helper_receives_it()
    {
        var category = new Category { Id = CategoryId, Name = "Category Four", LastKnownImageCount = 999, TotalPages = 999, };

        var okResponse = Substitute.For<IResponse>();
        okResponse.Ok.Returns(true);

        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns(Task.FromResult<IResponse?>(okResponse));

        var headerLocator = Substitute.For<ILocator>();
        headerLocator.TextContentAsync().Returns(Task.FromResult<string?>(HeaderText));
        page.GetByText(Arg.Is<string>(text => text.Contains("Wallpapers found")), Arg.Any<PageGetByTextOptions>()).Returns(headerLocator);

        var noLinksLocator = Substitute.For<ILocator>();
        noLinksLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([]));
        page.GetByRole(AriaRole.Link, Arg.Any<PageGetByRoleOptions>()).Returns(noLinksLocator);

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var searchConfiguration = new SearchConfigurationBuilder { SearchCategories = [category,], }.Build();
        var scrapeConfiguration = new ScrapeConfigurationBuilder
        {
            SearchConfiguration = searchConfiguration,
            ScrapeDirectories = new(RootDirectory, "base-save-directory", BaseDirectory, "base-directory-famous", "initial-sub-directory"),
        }.Build();

        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        var searchResultsPage = new SearchResultsPage(playwrightService);
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var imagePage = new ImagePage(playwrightService, scrapeConfiguration, new(), new());
        var fileClassificationService = new FileClassificationService(contextFactory, new LoggerConfiguration().CreateLogger());
        var imagePageService = new ImagePageService(imagePage, Substitute.For<IFileDetailRepository>(), fileClassificationService, scrapeConfiguration, System.TimeProvider.System, new LoggerConfiguration().CreateLogger(), Substitute.For<IDirectoryHelper>(), new(), new NoOpDelayStrategy(), Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IScrapedTagRepository>(), Substitute.For<IImageDimensionReader>());
        var directoryHelper = Substitute.For<IDirectoryHelper>();

        var sut = new SearchWorkflow(searchResultsPage, scrapeConfiguration, configurationSaver, imagePageService, directoryHelper, Substitute.For<ILogger>(), new NoOpDelayStrategy(), System.TimeProvider.System, new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy()));

        await sut.RunAsync(TestContext.Current.CancellationToken);

        List<string> expectedDirectoryPath = [Path.Combine(RootDirectory, BaseDirectory, ExpectedSubDirectoryName),];

        directoryHelper.Received(1).CreateDirectoryIfRequired(Arg.Is<List<string>>(directories => directories.SequenceEqual(expectedDirectoryPath)));
    }
}
