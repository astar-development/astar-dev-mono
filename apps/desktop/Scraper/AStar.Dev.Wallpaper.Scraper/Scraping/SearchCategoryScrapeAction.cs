using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Scrapes the search category listing and ensures each observed category exists in the file
///     classification taxonomy, ready to be used for tagging scraped images.
/// </summary>
public sealed class SearchCategoryScrapeAction(
    IScrapeContextReader contextReader,
    ISearchCategoryWriter searchCategoryWriter,
    IWallpaperCountReader countReader,
    ISearchCategoryReader searchCategoryReader,
    IWallpaperHrefCollector hrefCollector,
    IWallpaperPageVisitor wallpaperVisitor,
    IWallpaperThumbnailPublisher thumbnailPublisher,
    Clock clock) : IScrapeAction
{
    private const int ImagesPerPage = 24;

    /// <inheritdoc />
    public string Name => "Scrape Search Categories";

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

            await scrapeContext.Categories.ForEachAsync(category => VisitCategoryAsync(new CategoryScrapeContext(page, progress, scrapeContext, category, scrapeContext.FileClassifications), cancellationToken));

            progress.Report($"{clock():T} Completed scraping all categories");
            return UnitFp.Instance;
        }, cancellationToken);

    private async Task VisitCategoryAsync(CategoryScrapeContext context, CancellationToken cancellationToken)
    {
        context.Progress.Report($"{clock():T} Visiting category: <Run FontSize=\"18\">{context.Category.Name}</Run>");
        await context.Page.GotoAsync(context.Category.SearchUrl);

        int wallpaperCount = await countReader.ReadAsync(context.Page, cancellationToken);
        context.Progress.Report($"{clock():T} Number of wallpapers found for category: <Run FontSize=\"18\">{context.Category.Name}</Run> is <Span Foreground=\"Green\"><Run FontSize=\"18\">{wallpaperCount}</Run></Span>");

        int pageCount = (int)Math.Ceiling(wallpaperCount / (double)ImagesPerPage);
        var progressOption = await searchCategoryReader.GetProgressAsync(context.Category.Name, cancellationToken);
        bool isFullyVisited = progressOption.MapOrDefault(progress => progress.LastKnownImageCount == wallpaperCount && progress.LastPageVisited == pageCount, false);

        if (isFullyVisited)
        {
            context.Progress.Report($"{clock():T} Category: <Run FontSize=\"18\">{context.Category.Name}</Run> already fully visited (image count: <Span Foreground=\"Green\"><Run FontSize=\"18\">{wallpaperCount}</Run></Span>)");
            thumbnailPublisher.PublishCategorySkipped(context.Category.Name);
            await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 2_000, cancellationToken);

            return;
        }

        context.Progress.Report($"{clock():T} Category: <Run FontSize=\"18\">{context.Category.Name}</Run> has <Span Foreground=\"Green\"><Run FontSize=\"18\">{wallpaperCount}</Run></Span> wallpapers, need to get all <Span Foreground=\"Green\">{pageCount}</Span> pages for this category");
        await Enumerable.Range(1, pageCount).ForEachAsync(pageNumber => VisitCategoryPageAsync(context, pageNumber, pageCount, wallpaperCount, cancellationToken));
    }

    private async Task VisitCategoryPageAsync(CategoryScrapeContext context, int pageNumber, int pageCount, int wallpaperCount, CancellationToken cancellationToken)
    {
        SearchCategoryDto searchCategory = new(context.Category.Name, context.Category.IsFamous, context.Category.IsInternet, pageCount, wallpaperCount, pageNumber);
        (await searchCategoryWriter.WriteAsync(searchCategory, cancellationToken)).Match(
            onSuccess: _ => UnitFp.Instance,
            onFailure: error =>
            {
                context.Progress.Report($"{clock():T} Failed to persist scrape progress for category: <Run FontSize=\"18\">{context.Category.Name}</Run>, error: <Span Foreground=\"Red\">{error}</Span>");

                return UnitFp.Instance;
            });

        string pageUrl = $"{context.Category.SearchUrl}&page={pageNumber}";
        context.Progress.Report($"{clock():T} Visiting category: <Run FontSize=\"18\">{context.Category.Name}</Run>, page <Span Foreground=\"Green\">{pageNumber}</Span> of <Span Foreground=\"Green\">{pageCount}</Span>");
        await context.Page.GotoAsync(pageUrl);

        var hrefs = await hrefCollector.CollectAsync(context.Page, cancellationToken);

        await hrefs.ForEachAsync(href => wallpaperVisitor.VisitAsync(context, href, cancellationToken));

        await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);
    }
}
