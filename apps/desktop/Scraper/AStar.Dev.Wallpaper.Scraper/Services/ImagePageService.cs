using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Pages;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using AStar.Dev.Wallpaper.Scraper.Support;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class ImagePageService(
    ImagePage imagePage,
    IFileDetailRepository fileDetailRepository,
    FileClassificationService fileClassificationService,
    TimeProvider timeProvider,
    Logger logger,
    IDirectoryHelper directoryHelper,
    IDelayStrategy delayStrategy,
    ImageDownloader imageDownloader,
    ImagePersistence imagePersistence,
    IFileClassificationCategoriesRepository scrapedTagRepository)
{
    public async Task<Result<Unit, ScrapeError>> GetTheImagePagesAsync(IReadOnlyCollection<string> imagePageLinks, string categoryId, string name, CancellationToken cancellationToken = default)
    {
        var pageData = await fileClassificationService.LoadPageClassificationDataAsync(categoryId, cancellationToken).ConfigureAwait(false);

        Result<Unit, ScrapeError> aggregate = Unit.Value;

        foreach (string pageLink in imagePageLinks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            aggregate = await aggregate.BindAsync(_ => ProcessLinkAsync(pageLink, name, pageData, cancellationToken)).ConfigureAwait(false);
        }

        return aggregate;
    }

    public async Task<Result<Unit, ScrapeError>> ProcessImagePageAsync(string pageLink, string categoryName, PageClassificationData pageData, CancellationToken cancellationToken)
    {
        await delayStrategy.DelayAsync(DelayKind.BeforeImage, cancellationToken).ConfigureAwait(false);

        return await imagePage.GetImageFromPageAsync(pageLink, categoryName)
            .BindAsync(outcome => HandleOutcomeAsync(outcome, categoryName, pageData, cancellationToken))
            .ConfigureAwait(false);
    }

    private Task<Result<Unit, ScrapeError>> ProcessLinkAsync(string pageLink, string categoryName, PageClassificationData pageData, CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(pageLink);

        return fileDetailRepository.ExistsAsync(fileName, cancellationToken)
            .BindAsync(alreadyExists => alreadyExists ? SkipExistingImageAsync(fileName, cancellationToken) : ProcessImagePageAsync(pageLink, categoryName, pageData, cancellationToken));
    }

    private async Task<Result<Unit, ScrapeError>> SkipExistingImageAsync(string fileName, CancellationToken cancellationToken)
    {
        logger.Information("Not downloading {fileName} as we already have it...{Timestamp:HH:mm:ss:fff} (UTC)", fileName, timeProvider.GetUtcNow());
        await delayStrategy.DelayAsync(DelayKind.ImageAlreadyDownloaded, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }

    private Task<Result<Unit, ScrapeError>> HandleOutcomeAsync(ImagePageOutcome outcome, string categoryName, PageClassificationData pageData, CancellationToken cancellationToken)
        => SaveScrapedTagsAsync(outcome, cancellationToken)
            .BindAsync(_ => outcome switch
            {
                SkippedImage skipped => Task.FromResult(LogSkippedImage(categoryName, skipped)),
                ScrapedImage scraped => DownloadAndPersistAsync(scraped, pageData, cancellationToken),
                _ => throw new InvalidOperationException("Unexpected image page outcome."),
            });

    private Task<Result<Unit, ScrapeError>> SaveScrapedTagsAsync(ImagePageOutcome outcome, CancellationToken cancellationToken)
    {
        var rawTags = outcome switch
        {
            ScrapedImage scraped => scraped.RawTags,
            SkippedImage skipped => skipped.RawTags,
            _ => throw new InvalidOperationException("Unexpected image page outcome."),
        };

        return scrapedTagRepository.SaveAsync([.. rawTags.Where(tag => !string.IsNullOrWhiteSpace(tag.Category)),], cancellationToken);
    }

    private Result<Unit, ScrapeError> LogSkippedImage(string categoryName, SkippedImage skipped)
    {
        logger.Information("Skipping {Name} with Tags: {Tags}", categoryName, string.Join(", ", skipped.Tags));

        return Unit.Value;
    }

    private async Task<Result<Unit, ScrapeError>> DownloadAndPersistAsync(ScrapedImage scraped, PageClassificationData pageData, CancellationToken cancellationToken)
    {
        var directoryName = directoryHelper.CreateDirectoryIfRequired([.. scraped.DirectorySegments,]);
        string filename = ScrapedFileNameFactory.Create(scraped.FilePrefix, scraped.ImageUrl);
        string imageNameWithPath = directoryName.Value.CombinePath(filename);

        return await imageDownloader.DownloadAsync(scraped.ImageUrl, cancellationToken)
            .BindAsync(image => imagePersistence.SaveAndPersistAsync(image, imageNameWithPath, filename, directoryName, cancellationToken))
            .BindAsync(fileDetail => fileClassificationService.ClassifyAsync(fileDetail, pageData, scraped.Tags, cancellationToken))
            .ConfigureAwait(false);
    }
}
