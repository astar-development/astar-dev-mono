using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class ImagePageService(
    ImagePage imagePage,
    IFileDetailRepository fileDetailRepository,
    FileClassificationService fileClassificationService,
    ScrapeConfiguration scrapeConfiguration,
    TimeProvider timeProvider,
    Logger logger,
    IDirectoryHelper directoryHelper,
    ImageBroadcaster imageBroadcaster,
    IDelayStrategy delayStrategy,
    IImageRetriever imageRetriever,
    IImageSaver imageSaver,
    IFileSystem fileSystem,
    IScrapedTagRepository scrapedTagRepository,
    IImageDimensionReader imageDimensionReader)
{
    private const int LoggedPathTailLength = 50;

    /// <summary>Retained for constructor-signature stability. The image-pause delay it previously drove now lives in <see cref="RandomDelayStrategy" />, which is configured with its own <see cref="Models.ScrapeConfiguration" />.</summary>
    private readonly ScrapeConfiguration scrapeConfiguration = scrapeConfiguration;

    /// <summary>Retained for constructor-signature stability. File size is now computed from the downloaded bytes directly, so a saved image's <see cref="IFileSystem" /> entry no longer needs to be re-read.</summary>
    private readonly IFileSystem fileSystem = fileSystem;

    public async Task<Result<Unit, ScrapeError>> GetTheImagePagesAsync(IReadOnlyCollection<string> imagePageLinks, string categoryId, string name, CancellationToken ct = default)
    {
        var pageData = await fileClassificationService.LoadPageClassificationDataAsync(categoryId, ct).ConfigureAwait(false);

        foreach (string pageLink in imagePageLinks)
        {
            ct.ThrowIfCancellationRequested();
            string fileName = Path.GetFileName(pageLink);

            if (await fileDetailRepository.ExistsAsync(fileName).ConfigureAwait(false))
            {
                logger.Information("Not downloading {fileName} as we already have it...{Timestamp:HH:mm:ss:fff} (UTC)", fileName, timeProvider.GetUtcNow());
                await delayStrategy.DelayAsync(DelayKind.ImageAlreadyDownloaded, ct).ConfigureAwait(false);
                continue;
            }

            var pageResult = await ProcessImagePageAsync(pageLink, name, pageData, ct).ConfigureAwait(false);
            var pageFailed = pageResult.Match(_ => false, _ => true);

            if (pageFailed) return pageResult;
        }

        return Unit.Value;
    }

    public async Task<Result<Unit, ScrapeError>> ProcessImagePageAsync(string pageLink, string categoryName, PageClassificationData pageData, CancellationToken ct)
    {
        await delayStrategy.DelayAsync(DelayKind.BeforeImage, ct).ConfigureAwait(false);

        return await imagePage.GetImageFromPageAsync(pageLink, categoryName)
            .BindAsync(outcome => HandleOutcomeAsync(outcome, categoryName, pageData, ct))
            .ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> HandleOutcomeAsync(ImagePageOutcome outcome, string categoryName, PageClassificationData pageData, CancellationToken ct)
    {
        await SaveScrapedTagsAsync(outcome).ConfigureAwait(false);

        return outcome switch
        {
            SkippedImage skipped => LogSkippedImage(categoryName, skipped),
            ScrapedImage scraped => await DownloadAndPersistAsync(scraped, pageData, ct).ConfigureAwait(false),
            _ => throw new InvalidOperationException("Unexpected image page outcome."),
        };
    }

    private Task SaveScrapedTagsAsync(ImagePageOutcome outcome)
    {
        var rawTags = outcome switch
        {
            ScrapedImage scraped => scraped.RawTags,
            SkippedImage skipped => skipped.RawTags,
            _ => throw new InvalidOperationException("Unexpected image page outcome."),
        };

        return scrapedTagRepository.SaveAsync([.. rawTags.Where(tag => !string.IsNullOrWhiteSpace(tag.Category)),]);
    }

    private Result<Unit, ScrapeError> LogSkippedImage(string categoryName, SkippedImage skipped)
    {
        logger.Information("Skipping {Name} with Tags: {Tags}", categoryName, string.Join(", ", skipped.Tags));

        return Unit.Value;
    }

    private async Task<Result<Unit, ScrapeError>> DownloadAndPersistAsync(ScrapedImage scraped, PageClassificationData pageData, CancellationToken ct)
    {
        var directoryName = directoryHelper.CreateDirectoryIfRequired([.. scraped.DirectorySegments,]);
        string filename = ScrapedFileNameFactory.Create(scraped.FilePrefix, scraped.ImageUrl);
        string imageNameWithPath = directoryName.Value.CombinePath(filename);

        return await RetryExtensions.RetryOnceAsync(
                () => imageRetriever.GetImageAsync(scraped.ImageUrl, ct),
                () => delayStrategy.DelayAsync(DelayKind.Retry, ct))
            .BindAsync(image => SaveAndPersistAsync(image, imageNameWithPath, filename, directoryName, scraped, pageData, ct))
            .ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> SaveAndPersistAsync(byte[] image, string imageNameWithPath, string filename, DirectoryName directoryName, ScrapedImage scraped, PageClassificationData pageData, CancellationToken ct)
    {
        logger.Information("About to save {filename} to ...{imageNameWithPath} as we don't appear to have it.", filename, TruncatedForLogging(imageNameWithPath));

        return await imageSaver.SaveAsync(image, imageNameWithPath)
            .TapAsync(_ => imageBroadcaster.Broadcast(imageNameWithPath))
            .BindAsync(_ => PersistFileDetailAsync(image, imageNameWithPath, filename, directoryName, scraped, pageData, ct))
            .ConfigureAwait(false);
    }

    private async Task<Result<Unit, ScrapeError>> PersistFileDetailAsync(byte[] image, string imageNameWithPath, string filename, DirectoryName directoryName, ScrapedImage scraped, PageClassificationData pageData, CancellationToken ct)
    {
        var fileDetail = new FileDetailEntity
        {
            DirectoryName = directoryName,
            FileName = new FileName(filename),
            FileSize = image.Length,
            IsImage = filename.IsImage()
        };

        ApplyImageDimensions(fileDetail, image, imageNameWithPath);

        await fileDetailRepository.AddAsync(fileDetail).ConfigureAwait(false);

        return await fileClassificationService.ClassifyAsync(fileDetail, pageData, scraped.Tags, ct).ConfigureAwait(false);
    }

    private void ApplyImageDimensions(FileDetailEntity fileDetail, byte[] image, string imageNameWithPath)
        => imageDimensionReader.Read(image, imageNameWithPath)
            .Tap(
                dimensions =>
                {
                    fileDetail.Width = dimensions.Width;
                    fileDetail.Height = dimensions.Height;
                    fileDetail.ImageDetail = new ImageDetailEntity { Width = dimensions.Width, Height = dimensions.Height };
                },
                error => logger.Warning("Could not read image dimensions for {imageNameWithPath}: {Message}", TruncatedForLogging(imageNameWithPath), error.Message));

    private static string TruncatedForLogging(string imageNameWithPath)
        => imageNameWithPath.Length > LoggedPathTailLength ? imageNameWithPath[^LoggedPathTailLength..] : imageNameWithPath;
}
