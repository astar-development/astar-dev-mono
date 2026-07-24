using System.IO.Abstractions;
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
    ITagReader tagReader,
    IWallpaperImageLocator imageLocator,
    IWallpaperImageDownloader imageDownloader,
    IImageDimensionsReader dimensionsReader,
    IWallpaperFileStore fileStore,
    IWallpaperCategoryRegistrar categoryRegistrar,
    IWallpaperFileClassificationRepository fileClassificationRepository,
    IWallpaperThumbnailPublisher thumbnailPublisher,
    Clock clock,
    IFileSystem fileSystem) : IScrapeAction
{
    private const int ImagesPerPage = 24;
    private const int WallpaperPageTimeoutMilliseconds = 30_000;
    private const int ShortDelayForImageSkipInMilliseconds = 125;

    /// <inheritdoc />
    public string Name => "Scrape Search Categories";

    /// <inheritdoc />
    public async Task<Exceptional<Unit>> ExecuteAsync(IPage page, IProgress<string> progress, CancellationToken cancellationToken) =>
        await Try.RunAsync(async () =>
        {
            var scrapeContext = await contextReader.ReadAsync(cancellationToken);

            await scrapeContext.Categories.ForEachAsync(category => VisitCategoryAsync(new CategoryScrapeContext(page, progress, scrapeContext, category, scrapeContext.FileClassifications), cancellationToken));

            progress.Report($"{clock():T} Completed scraping all categories");
            return Unit.Instance;
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
            onSuccess: _ => Unit.Instance,
            onFailure: error =>
            {
                context.Progress.Report($"{clock():T} Failed to persist scrape progress for category: <Run FontSize=\"18\">{context.Category.Name}</Run>, error: <Span Foreground=\"Red\">{error}</Span>");

                return Unit.Instance;
            });

        string pageUrl = $"{context.Category.SearchUrl}&page={pageNumber}";
        context.Progress.Report($"{clock():T} Visiting category: <Run FontSize=\"18\">{context.Category.Name}</Run>, page <Span Foreground=\"Green\">{pageNumber}</Span> of <Span Foreground=\"Green\">{pageCount}</Span>");
        await context.Page.GotoAsync(pageUrl);

        var hrefs = await hrefCollector.CollectAsync(context.Page, cancellationToken);

        await hrefs.ForEachAsync(href => VisitWallpaperAsync(context, href, cancellationToken));

        await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);
    }

    private async Task VisitWallpaperAsync(CategoryScrapeContext context, string href, CancellationToken cancellationToken)
    {
        string wallpaperId = Path.GetFileName(href);

        if (await fileClassificationRepository.IsAlreadyDownloadedAsync(wallpaperId, cancellationToken))
        {
            context.Progress.Report($"{clock():T} Skipping wallpaper page: <Span Foreground=\"Green\">{href}</Span> as we already have it downloaded");
            await Task.Delay(ShortDelayForImageSkipInMilliseconds, cancellationToken);

            return;
        }

        context.Progress.Report($"{clock():T} Visiting wallpaper page: <Span Foreground=\"Green\">{href}</Span>");
        var response = await context.Page.GotoAsync(href, new PageGotoOptions { Timeout = WallpaperPageTimeoutMilliseconds, });

        if (response is not { Ok: true })
        {
            context.Progress.Report($"{clock():T} Failed to load wallpaper page: <Span Foreground=\"Red\">{href}</Span>, status: {response?.Status}");
            await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);

            return;
        }

        var tags = await tagReader.ReadAsync(context.Page, cancellationToken);
        var curation = TagCurator.Curate(tags, context.ScrapeContext.ModelsToIgnore, context.ScrapeContext.TagsToIgnore);
        string directoryPath = WallpaperDirectoryResolver.Resolve(context.ScrapeContext.Directories, curation.Kept, context.Category, context.FileClassifications, fileSystem);

        var imageUrlOption = await imageLocator.LocateAsync(context.Page, cancellationToken);

        await imageUrlOption.MatchAsync(
            onSomeAsync: imageUrl => DownloadWallpaperAsync(context, new WallpaperDownloadContext(imageUrl, directoryPath, curation.Kept), cancellationToken),
            onNone: () =>
            {
                context.Progress.Report($"{clock():T} Failed to get wallpaper image URL for page: <Span Foreground=\"Red\">{href}</Span>");

                return Unit.Instance;
            });
    }

    private async Task<Unit> DownloadWallpaperAsync(CategoryScrapeContext context, WallpaperDownloadContext download, CancellationToken cancellationToken) =>
        (await Try.RunAsync(async () =>
        {
            await categoryRegistrar.EnsureCategoriesExistAsync(download.Tags, cancellationToken);

            string fileName = Path.GetFileName(download.ImageUrl);

            return await (await imageDownloader.DownloadAsync(context.Page, download.ImageUrl, context.Category.Name, download.Tags.Select(tag => tag.Tag).ToList(), cancellationToken)).MatchAsync(
                onSuccess: async imageBytes =>
                {
                    var savedFile = await fileStore.SaveAsync(download.DirectoryPath, fileName, imageBytes, cancellationToken);
                    context.Progress.Report($"{clock():T} Downloaded wallpaper image from URL: <Span Foreground=\"Green\">{download.ImageUrl}</Span>, size: <Span Foreground=\"Green\">{imageBytes.Length}</Span> bytes");
                    var dimensions = dimensionsReader.Read(imageBytes);

                    await fileClassificationRepository.RecordAsync(download.Tags, download.ImageUrl, download.DirectoryPath, savedFile.SizeBytes, dimensions, cancellationToken);
                    await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);

                    return Unit.Instance;
                },
                onFailure: exception =>
                {
                    context.Progress.Report($"{clock():T} Failed to download wallpaper image from URL: {download.ImageUrl}, error: <Span Foreground=\"Red\">{exception.Message}</Span>");

                    return Unit.Instance;
                });
        }, cancellationToken)).Match(
            onSuccess: unit => unit,
            onFailure: exception =>
            {
                context.Progress.Report($"{clock():T} Failed to process wallpaper image from URL: <Span Foreground=\"Red\">{download.ImageUrl}</Span>, error: <Span Foreground=\"Red\">{exception.Message}</Span>");

                return Unit.Instance;
            });
}
