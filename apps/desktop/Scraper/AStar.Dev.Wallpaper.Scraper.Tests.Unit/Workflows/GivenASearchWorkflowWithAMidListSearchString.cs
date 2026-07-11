using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Pages;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;
using AStar.Dev.Wallpaper.Scraper.Tests.Unit.TestData;
using AStar.Dev.Wallpaper.Scraper.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Serilog;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Workflows;

public sealed class GivenASearchWorkflowWithAMidListSearchString
{
    private const string SearchStringPrefix = "https://example.test/search/";
    private const string SearchStringSuffix = "/?page=";
    private const string CategoryBeforeMatchId = "before-cat";
    private const string MatchingCategoryId = "match-cat";

    [Fact]
    public async Task when_run_then_categories_before_the_matching_category_are_not_visited()
    {
        var categoryBeforeMatch = new Category { Id = CategoryBeforeMatchId, Name = "Before", LastKnownImageCount = 10, TotalPages = 1, };
        var matchingCategory = new Category { Id = MatchingCategoryId, Name = "Match", LastKnownImageCount = 10, TotalPages = 1, };
        string matchingSearchString = $"{SearchStringPrefix}{matchingCategory.Id}{SearchStringSuffix}";

        var upToDateResponse = Substitute.For<IResponse>();
        upToDateResponse.Ok.Returns(true);

        var page = Substitute.For<IPage>();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns(Task.FromResult<IResponse?>(upToDateResponse));

        var headerLocator = Substitute.For<ILocator>();
        headerLocator.TextContentAsync().Returns(Task.FromResult<string?>("10 Wallpapers found for #Match"));
        page.GetByText(Arg.Is<string>(text => text.Contains("Wallpapers found")), Arg.Any<PageGetByTextOptions>()).Returns(headerLocator);

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var searchConfiguration = new SearchConfigurationBuilder
        {
            SearchCategories = [categoryBeforeMatch, matchingCategory,],
            SearchString = matchingSearchString,
            SearchStringPrefix = SearchStringPrefix,
            SearchStringSuffix = SearchStringSuffix,
        }.Build();

        var scrapeConfiguration = new ScrapeConfigurationBuilder { SearchConfiguration = searchConfiguration, }.Build();

        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        var searchResultsPage = new SearchResultsPage(playwrightService);
        var configurationSaver = new ConfigurationSaver(scrapeConfiguration, new LoggerConfiguration().CreateLogger(), contextFactory);
        var imagePage = new ImagePage(playwrightService, scrapeConfiguration, new(), new());
        var fileClassificationService = new FileClassificationService(contextFactory);
        var imagePageService = new ImagePageService(imagePage, RepositoryTestDoubles.BuildFileDetailRepository(), fileClassificationService, scrapeConfiguration, System.TimeProvider.System, new LoggerConfiguration().CreateLogger(), Substitute.For<IDirectoryHelper>(), new(), new NoOpDelayStrategy(), Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), RepositoryTestDoubles.BuildScrapedTagRepository(), Substitute.For<IImageDimensionReader>());

        var sut = new SearchWorkflow(searchResultsPage, scrapeConfiguration, configurationSaver, imagePageService, Substitute.For<IDirectoryHelper>(), Substitute.For<ILogger>(), new NoOpDelayStrategy(), System.TimeProvider.System, new PagedScrapeRunner(configurationSaver, new NoOpDelayStrategy()));

        await sut.RunAsync(TestContext.Current.CancellationToken);

        await page.DidNotReceive().GotoAsync(Arg.Is<string>(url => url.Contains(CategoryBeforeMatchId)), Arg.Any<PageGotoOptions>());
    }
}
