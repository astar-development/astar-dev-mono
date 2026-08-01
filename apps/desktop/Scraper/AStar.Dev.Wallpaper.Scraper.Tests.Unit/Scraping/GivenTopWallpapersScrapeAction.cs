using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenTopWallpapersScrapeAction
{
    private readonly IScrapeContextReader contextReader = Substitute.For<IScrapeContextReader>();
    private readonly ITopWallpapersPageInfoReader pageInfoReader = Substitute.For<ITopWallpapersPageInfoReader>();
    private readonly IWallpaperHrefCollector hrefCollector = Substitute.For<IWallpaperHrefCollector>();
    private readonly IWallpaperPageVisitor wallpaperVisitor = Substitute.For<IWallpaperPageVisitor>();
    private readonly ISearchConfigurationProgressWriter progressWriter = Substitute.For<ISearchConfigurationProgressWriter>();
    private readonly IProgress<string> progress = Substitute.For<IProgress<string>>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly Clock clock = () => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private const string TopWallpapersUrl = "https://wallhaven.cc/search?categories=001&purity=111&topRange=3M&sorting=toplist&order=desc&page=";

    private static ScrapeContext CreateContext(int startingPageNumber = 0) => new(
        [],
        [],
        [],
        new DirectoryLayout("/root", "/base", "/famous"), [], new SearchConfigurationEntity
        {
            Id = 1,
            ImagePauseInSeconds = 1,
            TopWallpapersStartingPageNumber = startingPageNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    [Fact]
    public async Task when_no_starting_page_is_configured_then_the_run_starts_at_page_one()
    {
        var sut = CreateSut(pageCount: 1);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await page.Received(2).GotoAsync($"{TopWallpapersUrl}1");
    }

    [Fact]
    public async Task when_a_starting_page_is_configured_then_the_run_starts_from_that_page()
    {
        var sut = CreateSut(context: CreateContext(3), pageCount: 5);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await page.Received(2).GotoAsync($"{TopWallpapersUrl}3");
        await page.Received(1).GotoAsync($"{TopWallpapersUrl}4");
        await page.Received(1).GotoAsync($"{TopWallpapersUrl}5");
    }

    [Fact]
    public async Task when_navigating_to_the_configured_starting_page_throws_then_it_falls_back_to_page_one()
    {
        var sut = CreateSut(context: CreateContext(7), pageCount: 1, startingPageNavigationException: new PlaywrightException("boom"));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await page.Received().GotoAsync($"{TopWallpapersUrl}7");
        await page.Received().GotoAsync($"{TopWallpapersUrl}1");
    }

    [Fact]
    public async Task when_the_page_count_is_read_then_progress_is_persisted_before_and_during_the_run()
    {
        var sut = CreateSut(pageCount: 3);

        await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        await progressWriter.Received().WriteTopWallpapersProgressAsync(1, 3, Arg.Any<CancellationToken>());
        await progressWriter.Received().WriteTopWallpapersProgressAsync(2, 3, Arg.Any<CancellationToken>());
        await progressWriter.Received().WriteTopWallpapersProgressAsync(3, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_page_of_hrefs_is_collected_then_each_href_is_visited()
    {
        string[] hrefs = ["https://wallhaven.cc/w/abc123", "https://wallhaven.cc/w/def456"];
        var sut = CreateSut(pageCount: 1, hrefs: hrefs);

        await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        await wallpaperVisitor.Received().VisitAsync(Arg.Any<CategoryScrapeContext>(), "https://wallhaven.cc/w/abc123", Arg.Any<CancellationToken>());
        await wallpaperVisitor.Received().VisitAsync(Arg.Any<CategoryScrapeContext>(), "https://wallhaven.cc/w/def456", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reading_the_scrape_context_fails_then_a_failure_result_is_returned_instead_of_throwing()
    {
        var sut = CreateSut(pageCount: 1, contextReaderException: new InvalidOperationException("Sequence contains no elements"));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<FunctionalParadigm.UnitFp>>();
    }

    private TopWallpapersScrapeAction CreateSut(
        ScrapeContext? context = null,
        int pageCount = 1,
        IReadOnlyList<string>? hrefs = null,
        Exception? contextReaderException = null,
        Exception? startingPageNavigationException = null)
    {
        if (contextReaderException is not null)
        {
            contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns<ScrapeContext>(_ => throw contextReaderException);
        }
        else
        {
            contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(context ?? CreateContext());
        }

        pageInfoReader.ReadPageCountAsync(page, Arg.Any<CancellationToken>()).Returns(pageCount);

        if (startingPageNavigationException is not null)
        {
            int startingPageNumber = (context ?? CreateContext()).SearchConfiguration.TopWallpapersStartingPageNumber is > 0 ? (context ?? CreateContext()).SearchConfiguration.TopWallpapersStartingPageNumber : 1;
            page.GotoAsync($"{TopWallpapersUrl}{startingPageNumber}").Returns<IResponse?>(_ => throw startingPageNavigationException);
        }

        if (hrefs is not null)
        {
            hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns(hrefs);
        }

        return new(contextReader, pageInfoReader, hrefCollector, wallpaperVisitor, progressWriter, clock);
    }
}
