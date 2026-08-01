using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Scrapes the Top Wallpapers listing, a paged section of the target site with no search category of its own.
/// </summary>
public sealed class TopWallpapersScrapeAction(
    IScrapeContextReader contextReader,
    ITopWallpapersPageInfoReader pageInfoReader,
    IWallpaperHrefCollector hrefCollector,
    IWallpaperPageVisitor wallpaperVisitor,
    ISearchConfigurationProgressWriter progressWriter,
    Clock clock) : ITopWallpapersScrapeAction
{
    private const int FirstPageNumber = 1;

    // TODO: no database field currently holds the Top Wallpapers URL - hard-coded until one is added.
    private const string TopWallpapersUrl = "https://wallhaven.cc/search?categories=001&purity=111&topRange=3M&sorting=toplist&order=desc&page=";

    private static readonly ScrapeCategory pseudoCategoryTemplate = new("Top Wallpapers", TopWallpapersUrl, false, false);

    /// <inheritdoc />
    public string Name => "Scrape Top Wallpapers";

    /// <inheritdoc />
    public async Task<Exceptional<UnitFp>> ExecuteAsync(IPage page, IProgress<string> progress, CancellationToken cancellationToken) =>
        await Try.RunAsync(async () =>
        {
            var scrapeContext = await contextReader.ReadAsync(cancellationToken);
            var context = new CategoryScrapeContext(page, progress, scrapeContext, pseudoCategoryTemplate, scrapeContext.FileClassifications);

            int startingPage = scrapeContext.SearchConfiguration.TopWallpapersStartingPageNumber > 0 ? scrapeContext.SearchConfiguration.TopWallpapersStartingPageNumber : FirstPageNumber;
            await NavigateToStartingPageAsync(context, startingPage, cancellationToken);

            int pageCount = await pageInfoReader.ReadPageCountAsync(page, cancellationToken);
            context.Progress.Report($"{clock():T} There are a total of <Span Foreground=\"Green\">{pageCount}</Span> pages for Top Wallpapers");
            await progressWriter.WriteTopWallpapersProgressAsync(startingPage, pageCount, cancellationToken);

            await VisitPagesAsync(context, startingPage, pageCount, cancellationToken);

            context.Progress.Report($"{clock():T} Completed scraping Top Wallpapers");
            return UnitFp.Instance;
        }, cancellationToken);

    private static async Task NavigateToStartingPageAsync(CategoryScrapeContext context, int startingPage, CancellationToken cancellationToken)
    {
        try
        {
            await context.Page.GotoAsync($"{context.Category.SearchUrl}{startingPage}");
        }
        catch (Exception) when (startingPage != FirstPageNumber)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await context.Page.GotoAsync($"{context.Category.SearchUrl}{FirstPageNumber}");
        }
    }

    private async Task VisitPagesAsync(CategoryScrapeContext context, int startingPage, int pageCount, CancellationToken cancellationToken)
    {
        for (int pageNumber = startingPage; pageNumber <= pageCount; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await progressWriter.WriteTopWallpapersProgressAsync(pageNumber, pageCount, cancellationToken);

            context.Progress.Report($"{clock():T} Visiting Top Wallpapers, page <Span Foreground=\"Green\">{pageNumber}</Span> of <Span Foreground=\"Green\">{pageCount}</Span>");
            await context.Page.GotoAsync($"{context.Category.SearchUrl}{pageNumber}");

            var hrefs = await hrefCollector.CollectAsync(context.Page, cancellationToken);
            await hrefs.ForEachAsync(href => wallpaperVisitor.VisitAsync(context, href, cancellationToken));

            await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);
        }
    }
}
