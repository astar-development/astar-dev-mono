using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scraper.Scraping.Actions;
using AStar.Dev.Wallpaper.Scraper.Scraping.Categories;
using AStar.Dev.Wallpaper.Scraper.Scraping.Context;
using AStar.Dev.Wallpaper.Scraper.Scraping.Navigation;
using AStar.Dev.Wallpaper.Scraper.Scraping.SearchCategories;
using AStar.Dev.Wallpaper.Scraper.Scraping.Thumbnails;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping.Actions;

public sealed class GivenSearchCategoryScrapeAction
{
    private readonly IScrapeContextReader contextReader = Substitute.For<IScrapeContextReader>();
    private readonly ISearchCategoryWriter categoryWriter = Substitute.For<ISearchCategoryWriter>();
    private readonly IWallpaperCountReader countReader = Substitute.For<IWallpaperCountReader>();
    private readonly IWallpaperHrefCollector hrefCollector = Substitute.For<IWallpaperHrefCollector>();
    private readonly IWallpaperPageVisitor wallpaperVisitor = Substitute.For<IWallpaperPageVisitor>();
    private readonly IWallpaperThumbnailPublisher thumbnailPublisher = Substitute.For<IWallpaperThumbnailPublisher>();
    private readonly IProgress<string> progress = Substitute.For<IProgress<string>>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly Clock clock = () => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly ScrapeContext singleCategoryContext = new(
        [new ScrapeCategory("Nature", "https://wallhaven.cc/search?categories=1", false, false)],
        [],
        [],
        new DirectoryLayout("/root", "/base", "/famous"), [], new SearchConfigurationEntity
        {
            Id = 1,
            SearchStringPrefix = "https://wallhaven.cc/search?categories=",
            SearchStringSuffix = string.Empty,
            ImagePauseInSeconds = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    private static readonly ScrapeContext twoCategoryContext = new(
        [
            new ScrapeCategory("Nature", "https://wallhaven.cc/search?categories=1", false, false),
            new ScrapeCategory("Space", "https://wallhaven.cc/search?categories=2", false, false),
        ],
        [],
        [],
        new DirectoryLayout("/root", "/base", "/famous"), [], new SearchConfigurationEntity
        {
            Id = 1,
            SearchStringPrefix = "https://wallhaven.cc/search?categories=",
            SearchStringSuffix = string.Empty,
            ImagePauseInSeconds = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    [Fact]
    public async Task when_a_category_has_more_wallpapers_than_fit_on_one_page_then_progress_reports_the_page_count_and_a_success_result_is_returned()
    {
        var sut = CreateSut(clock, wallpaperCount: 50);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        progress.Received().Report(Arg.Is<string>(message => message.Contains("Visiting category: <Run FontSize=\"18\">Nature</Run>")));
        progress.Received().Report(Arg.Is<string>(message => message.Contains("need to get all <Span Foreground=\"Green\">3</Span> pages")));
    }

    [Fact]
    public async Task when_persisting_scrape_progress_fails_then_progress_reports_the_failure_and_the_page_is_still_visited()
    {
        var sut = CreateSut(clock, writerResult: Result.Failure<FunctionalParadigm.UnitFp, string>("No search category named 'Nature' exists to update."));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        progress.Received().Report(Arg.Is<string>(message => message.Contains("Failed to persist scrape progress for category: <Run FontSize=\"18\">Nature</Run>, error: <Span Foreground=\"Red\">No search category named 'Nature' exists to update.</Span>")));
        await page.Received().GotoAsync(Arg.Is<string>(url => url.Contains("&page=1")));
    }

    [Fact]
    public async Task when_multiple_categories_are_configured_then_each_category_is_visited_on_its_own_search_url()
    {
        var sut = CreateSut(clock, context: twoCategoryContext);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        progress.Received().Report(Arg.Is<string>(message => message.Contains("Visiting category: <Run FontSize=\"18\">Nature</Run>")));
        progress.Received().Report(Arg.Is<string>(message => message.Contains("Visiting category: <Run FontSize=\"18\">Space</Run>")));
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=2&page=1");
        await hrefCollector.Received(2).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_category_spans_multiple_pages_then_each_page_is_visited_and_hrefs_are_collected_per_page()
    {
        var sut = CreateSut(clock, wallpaperCount: 30);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=2");
        await hrefCollector.Received(2).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reading_the_scrape_context_fails_then_a_failure_result_is_returned_instead_of_throwing()
    {
        var sut = CreateSut(clock, contextReaderException: new InvalidOperationException("Sequence contains no elements"));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<FunctionalParadigm.UnitFp>>();
    }

    [Fact]
    public async Task when_a_page_of_hrefs_is_collected_then_each_href_is_visited()
    {
        string[] hrefs = ["https://wallhaven.cc/w/abc123", "https://wallhaven.cc/w/def456"];
        var sut = CreateSut(clock, hrefs: hrefs);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await wallpaperVisitor.Received().VisitAsync(Arg.Any<CategoryScrapeContext>(), "https://wallhaven.cc/w/abc123", Arg.Any<CancellationToken>());
        await wallpaperVisitor.Received().VisitAsync(Arg.Any<CategoryScrapeContext>(), "https://wallhaven.cc/w/def456", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_count_and_last_page_visited_both_match_the_stored_progress_then_the_category_is_skipped_and_progress_is_reported()
    {
        var sut = CreateSut(clock, wallpaperCount: 42, storedProgress: new SearchCategoryProgress(42, 2));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        progress.Received().Report(Arg.Is<string>(message => message.Contains("Category: <Run FontSize=\"18\">Nature</Run>") && message.Contains("already fully visited")));
        thumbnailPublisher.Received().PublishCategorySkipped("Nature");
        await page.DidNotReceive().GotoAsync(Arg.Is<string>(url => url.Contains("&page=")), Arg.Any<PageGotoOptions>());
        await hrefCollector.DidNotReceive().CollectAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_count_matches_but_the_last_page_visited_is_behind_the_calculated_page_count_then_the_category_is_scraped_normally()
    {
        var sut = CreateSut(clock, wallpaperCount: 42, storedProgress: new SearchCategoryProgress(42, 1));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=2");
        await hrefCollector.Received(2).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_count_differs_from_the_stored_count_then_the_category_is_scraped_normally()
    {
        var sut = CreateSut(clock, wallpaperCount: 24, storedProgress: new SearchCategoryProgress(20, 1));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await hrefCollector.Received(1).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reading_the_stored_progress_returns_none_then_the_category_is_scraped_normally()
    {
        var sut = CreateSut(clock, wallpaperCount: 24);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await hrefCollector.Received(1).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    private SearchCategoryScrapeAction CreateSut(
        Clock clock,
        ScrapeContext? context = null,
        int wallpaperCount = 1,
        SearchCategoryProgress? storedProgress = null,
        IReadOnlyList<string>? hrefs = null,
        Result<FunctionalParadigm.UnitFp, string>? writerResult = null,
        Exception? contextReaderException = null)
    {
        if (contextReaderException is not null)
        {
            contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns<ScrapeContext>(_ => throw contextReaderException);
        }
        else
        {
            contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(context ?? singleCategoryContext);
        }

        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(wallpaperCount);

        var searchCategoryReader = Substitute.For<ISearchCategoryReader>();
        searchCategoryReader.GetProgressAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(storedProgress is null ? Option.None<SearchCategoryProgress>() : new Option<SearchCategoryProgress>.Some(storedProgress));

        categoryWriter.WriteAsync(Arg.Any<SearchCategoryDto>(), Arg.Any<CancellationToken>())
            .Returns(writerResult ?? Result.Success<FunctionalParadigm.UnitFp, string>(FunctionalParadigm.UnitFp.Instance));

        if (hrefs is not null)
        {
            hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns(hrefs);
        }

        return new(contextReader, categoryWriter, countReader, searchCategoryReader, hrefCollector, wallpaperVisitor, thumbnailPublisher, clock);
    }
}
