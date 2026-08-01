using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scraper.Scraping.Actions;
using AStar.Dev.Wallpaper.Scraper.Scraping.Context;
using AStar.Dev.Wallpaper.Scraper.Scraping.Navigation;
using AStar.Dev.Wallpaper.Scraper.Scraping.SearchCategories;
using AStar.Dev.Wallpaper.Scraper.Scraping.Subscriptions;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping.Actions;

public sealed class GivenSubscriptionsScrapeAction
{
    private readonly IScrapeContextReader contextReader = Substitute.For<IScrapeContextReader>();
    private readonly ISubscriptionsPageInfoReader pageInfoReader = Substitute.For<ISubscriptionsPageInfoReader>();
    private readonly IWallpaperHrefCollector hrefCollector = Substitute.For<IWallpaperHrefCollector>();
    private readonly IWallpaperPageVisitor wallpaperVisitor = Substitute.For<IWallpaperPageVisitor>();
    private readonly ISearchConfigurationProgressWriter progressWriter = Substitute.For<ISearchConfigurationProgressWriter>();
    private readonly ISubscriptionsClearer subscriptionsClearer = Substitute.For<ISubscriptionsClearer>();
    private readonly IProgress<string> progress = Substitute.For<IProgress<string>>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly Clock clock = () => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private const string SubscriptionsUrl = "https://wallhaven.cc/subscriptions?page=";

    private static readonly ScrapeContext scrapeContext = new(
        [],
        [],
        [],
        new DirectoryLayout("/root", "/base", "/famous"), [], new SearchConfigurationEntity
        {
            Id = 1,
            Subscriptions = SubscriptionsUrl,
            ImagePauseInSeconds = 1,
            SubscriptionsStartingPageNumber = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    [Fact]
    public async Task when_a_run_starts_then_it_always_begins_at_page_one_regardless_of_stored_starting_page()
    {
        var sut = CreateSut(pageCount: 1);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await page.Received().GotoAsync($"{SubscriptionsUrl}1");
        await page.DidNotReceive().GotoAsync($"{SubscriptionsUrl}3");
    }

    [Fact]
    public async Task when_the_page_count_is_read_then_progress_is_persisted_before_and_during_the_run()
    {
        var sut = CreateSut(pageCount: 2);

        await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        await progressWriter.Received().WriteSubscriptionsProgressAsync(1, 2, Arg.Any<CancellationToken>());
        await progressWriter.Received().WriteSubscriptionsProgressAsync(2, 2, Arg.Any<CancellationToken>());
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
    public async Task when_all_pages_are_visited_then_all_subscriptions_are_cleared()
    {
        var sut = CreateSut(pageCount: 2);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await subscriptionsClearer.Received().ClearAllAsync(page, Arg.Any<CancellationToken>());
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Cleared all caught-up subscriptions")));
    }

    [Fact]
    public async Task when_there_are_no_new_subscription_wallpapers_then_subscriptions_are_not_cleared()
    {
        var sut = CreateSut(pageCount: 0);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.UnitFp>>();
        await subscriptionsClearer.DidNotReceive().ClearAllAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reading_the_scrape_context_fails_then_a_failure_result_is_returned_instead_of_throwing()
    {
        var sut = CreateSut(pageCount: 1, contextReaderException: new InvalidOperationException("Sequence contains no elements"));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<FunctionalParadigm.UnitFp>>();
    }

    private SubscriptionsScrapeAction CreateSut(
        int pageCount = 1,
        int imageCount = 0,
        IReadOnlyList<string>? hrefs = null,
        Exception? contextReaderException = null)
    {
        if (contextReaderException is not null)
        {
            contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns<ScrapeContext>(_ => throw contextReaderException);
        }
        else
        {
            contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(scrapeContext);
        }

        pageInfoReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(new SubscriptionsPageInfo(pageCount, imageCount));

        if (hrefs is not null)
        {
            hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns(hrefs);
        }

        return new(contextReader, pageInfoReader, hrefCollector, wallpaperVisitor, progressWriter, subscriptionsClearer, clock);
    }
}
