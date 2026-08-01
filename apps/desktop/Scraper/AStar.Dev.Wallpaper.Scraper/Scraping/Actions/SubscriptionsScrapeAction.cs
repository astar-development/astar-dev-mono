using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scraper.Scraping.Context;
using AStar.Dev.Wallpaper.Scraper.Scraping.Navigation;
using AStar.Dev.Wallpaper.Scraper.Scraping.SearchCategories;
using AStar.Dev.Wallpaper.Scraper.Scraping.Subscriptions;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.Actions;

/// <summary>
///     Scrapes the Subscriptions listing, a live inbox of new wallpapers from subscribed models with no search
///     category of its own. Always starts from page one, and clears the site's subscription list once fully
///     caught up.
/// </summary>
public sealed class SubscriptionsScrapeAction(
    IScrapeContextReader contextReader,
    ISubscriptionsPageInfoReader pageInfoReader,
    IWallpaperHrefCollector hrefCollector,
    IWallpaperPageVisitor wallpaperVisitor,
    ISearchConfigurationProgressWriter progressWriter,
    ISubscriptionsClearer subscriptionsClearer,
    Clock clock) : ISubscriptionsScrapeAction
{
    private const int FirstPageNumber = 1;
    private static readonly ScrapeCategory pseudoCategoryTemplate = new("Subscriptions", string.Empty, false, false);

    /// <inheritdoc />
    public string Name => "Scrape Subscribed Wallpapers";

    /// <inheritdoc />
    public async Task<Exceptional<UnitFp>> ExecuteAsync(IPage page, IProgress<string> progress, CancellationToken cancellationToken) =>
        await Try.RunAsync(async () =>
        {
            var scrapeContext = await contextReader.ReadAsync(cancellationToken);
            if(!Directory.Exists(scrapeContext.Directories.RootDirectory))
            {
                progress.Report($"{clock():T} Root directory '{scrapeContext.Directories.RootDirectory}' does not exist, cannot scrape categories");
                return UnitFp.Instance;
            }
            
            var category = pseudoCategoryTemplate with { SearchUrl = scrapeContext.SearchConfiguration.Subscriptions, };
            var context = new CategoryScrapeContext(page, progress, scrapeContext, category, scrapeContext.FileClassifications);

            await context.Page.GotoAsync($"{category.SearchUrl}{FirstPageNumber}");

            var pageInfo = await pageInfoReader.ReadAsync(page, cancellationToken);
            context.Progress.Report($"{clock():T} There are <Span Foreground=\"Green\">{pageInfo.ImageCount}</Span> new subscription wallpapers across <Span Foreground=\"Green\">{pageInfo.PageCount}</Span> pages");
            await progressWriter.WriteSubscriptionsProgressAsync(FirstPageNumber, pageInfo.PageCount, cancellationToken);

            await VisitPagesAsync(context, pageInfo.PageCount, cancellationToken);

            if (pageInfo.PageCount > 0)
            {
                await ClearCaughtUpSubscriptionsAsync(context, cancellationToken);
            }

            context.Progress.Report($"{clock():T} Completed scraping Subscriptions");
            return UnitFp.Instance;
        }, cancellationToken);

    private async Task VisitPagesAsync(CategoryScrapeContext context, int pageCount, CancellationToken cancellationToken)
    {
        for (int pageNumber = FirstPageNumber; pageNumber <= pageCount; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await progressWriter.WriteSubscriptionsProgressAsync(pageNumber, pageCount, cancellationToken);

            context.Progress.Report($"{clock():T} Visiting Subscriptions, page <Span Foreground=\"Green\">{pageNumber}</Span> of <Span Foreground=\"Green\">{pageCount}</Span>");
            await context.Page.GotoAsync($"{context.Category.SearchUrl}{pageNumber}");

            var hrefs = await hrefCollector.CollectAsync(context.Page, cancellationToken);
            await hrefs.ForEachAsync(href => wallpaperVisitor.VisitAsync(context, href, cancellationToken));

            await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);
        }
    }

    private async Task ClearCaughtUpSubscriptionsAsync(CategoryScrapeContext context, CancellationToken cancellationToken)
    {
        await context.Page.GotoAsync($"{context.Category.SearchUrl}{FirstPageNumber}");
        await subscriptionsClearer.ClearAllAsync(context.Page, cancellationToken);
        context.Progress.Report($"{clock():T} Cleared all caught-up subscriptions");
    }
}
