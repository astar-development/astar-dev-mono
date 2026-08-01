using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Playwright;
using AStar.Dev.Wallpaper.Scraper.Scraping.Categories;
using AStar.Dev.Wallpaper.Scraper.Scraping.Context;
using AStar.Dev.Wallpaper.Scraper.Scraping.ImageDownload;
using AStar.Dev.Wallpaper.Scraper.Scraping.Storage;
using AStar.Dev.Wallpaper.Scraper.Scraping.Tags;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.Navigation;

/// <inheritdoc cref="IWallpaperPageVisitor" />
public sealed class WallpaperPageVisitor(
    ITagReader tagReader,
    IWallpaperImageLocator imageLocator,
    IWallpaperImageDownloader imageDownloader,
    IImageDimensionsReader dimensionsReader,
    IWallpaperFileStore fileStore,
    IWallpaperCategoryRegistrar categoryRegistrar,
    IWallpaperFileClassificationRepository fileClassificationRepository,
    Clock clock,
    IFileSystem fileSystem) : IWallpaperPageVisitor
{
    private const int WallpaperPageTimeoutMilliseconds = 30_000;
    private const int ShortDelayForImageSkipInMilliseconds = 125;

    /// <inheritdoc />
    public async Task VisitAsync(CategoryScrapeContext context, string href, CancellationToken cancellationToken)
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
            onSomeAsync: imageUrl => DownloadAsync(context, new WallpaperDownloadContext(imageUrl, directoryPath, curation.Kept), cancellationToken),
            onNone: () =>
            {
                context.Progress.Report($"{clock():T} Failed to get wallpaper image URL for page: <Span Foreground=\"Red\">{href}</Span>");

                return UnitFp.Instance;
            });
    }

    private async Task<UnitFp> DownloadAsync(CategoryScrapeContext context, WallpaperDownloadContext download, CancellationToken cancellationToken) =>
        (await Try.RunAsync(async () =>
        {
            await categoryRegistrar.EnsureCategoriesExistAsync(download.Tags, cancellationToken);

            string fileName = Path.GetFileName(download.ImageUrl);

            return await (await imageDownloader.DownloadAsync(context.Page, download.ImageUrl, context.Category.Name, download.Tags.Select(tag => tag.Tag).ToList(), cancellationToken)).MatchAsync(
                onSuccess: async imageBytes =>
                {
                    var savedFile = await fileStore.SaveAsync(download.DirectoryPath, fileName, imageBytes, cancellationToken);
                    context.Progress.Report($"{clock():T} Downloaded wallpaper image from URL: <Span Foreground=\"Green\" FontSize=\"18\">{download.ImageUrl}</Span>, size: <Span Foreground=\"Green\" FontSize=\"18\">{imageBytes.Length:N0}</Span> bytes");
                    var dimensions = dimensionsReader.Read(imageBytes);

                    await fileClassificationRepository.RecordAsync(download.Tags, download.ImageUrl, download.DirectoryPath, savedFile.SizeBytes, dimensions, cancellationToken);
                    await Task.Delay(context.ScrapeContext.SearchConfiguration.ImagePauseInSeconds * 1_000, cancellationToken);

                    return UnitFp.Instance;
                },
                onFailure: exception =>
                {
                    context.Progress.Report($"{clock():T} Failed to download wallpaper image from URL: {download.ImageUrl}, error: <Span Foreground=\"Red\">{exception.Message}</Span>");

                    return UnitFp.Instance;
                });
        }, cancellationToken)).Match(
            onSuccess: unit => unit,
            onFailure: exception =>
            {
                context.Progress.Report($"{clock():T} Failed to process wallpaper image from URL: <Span Foreground=\"Red\">{download.ImageUrl}</Span>, error: <Span Foreground=\"Red\">{exception.Message}</Span>");

                return UnitFp.Instance;
            });
}
