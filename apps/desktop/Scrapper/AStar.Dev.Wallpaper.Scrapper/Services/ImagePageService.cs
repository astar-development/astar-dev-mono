using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Pages;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Support;
using Serilog.Core;
using SkiaSharp;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class ImagePageService(ImagePage imagePage, IFileDetailRepository fileDetailRepository, FileClassificationService fileClassificationService, ScrapeConfiguration scrapeConfiguration, TimeProvider timeProvider, Logger logger, ImageBroadcaster imageBroadcaster)
{
    public async Task GetTheImagePagesAsync(IReadOnlyCollection<string> imagePageLinks, string categoryId, string name, CancellationToken ct = default)
    {
        var pageData = await fileClassificationService.LoadPageClassificationDataAsync(categoryId, ct).ConfigureAwait(false);

        foreach (string pageLink in imagePageLinks)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                string fileName = Path.GetFileName(pageLink);

                if (await fileDetailRepository.ExistsAsync(fileName).ConfigureAwait(false))
                {
                    logger.Information("Not downloading {fileName} as we already have it...{Timestamp:HH:mm:ss:fff} (UTC)", fileName, timeProvider.GetUtcNow());
                    await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                    continue;
                }

                await ProcessImagePageAsync(pageLink, name, pageData, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.Warning(ex, "Failed to process {pageLink}, retrying after delay.", pageLink);
                await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
                await ProcessImagePageAsync(pageLink, name, pageData, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessImagePageAsync(string pageLink, string name, PageClassificationData pageData, CancellationToken ct)
    {
        int delay = Random.Shared.Next(scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds, scrapeConfiguration.SearchConfiguration.ImagePauseInSeconds + 4);
        await Task.Delay(TimeSpan.FromSeconds(delay), ct).ConfigureAwait(false);

        var result = await imagePage.GetImageFromPage(pageLink, name).ConfigureAwait(false);
        if (result.Skip || result.ImageUrl is null)
        {
            logger.Information("Skipping {Name} with Tags: {Tags}", name, string.Join(", ", result.Tags));
            return;
        }

        var directoryName = DirectoryHelper.CreateDirectoryIfRequired(result.DirectoryName);

        string filename = Path.GetFileName(result.ImageUrl).ToLowerInvariant();
        string fileNameCombined = !string.IsNullOrEmpty(result.FilePrefix) ? result.FilePrefix + " " + filename : filename;

        string imageNameWithPath = directoryName.Value.CombinePath(fileNameCombined.ToLowerInvariant());
        byte[] image = await ImageRetrieverHelper.GetTheImageAsync(result.ImageUrl).ConfigureAwait(false);
        logger.Information("About to save {filename} as {imageNameWithPath} as we don't appear to have it.", filename, imageNameWithPath);
        await ImageSaveHelper.SaveImage(image, imageNameWithPath).ConfigureAwait(false);
        imageBroadcaster.Broadcast(imageNameWithPath);

        var fileInfo = new FileInfo(imageNameWithPath);
        var fileDetail = new FileDetail
        {
            DirectoryName = directoryName,
            FileName = new FileName(filename),
            FileSize = fileInfo.Length,
            IsImage = filename.IsImage()
        };

        if (fileDetail.IsImage)
        {
            var imageDetail = SKImage.FromEncodedData(imageNameWithPath);
            if (imageDetail is not null)
            {
                fileDetail.Height = imageDetail.Height;
                fileDetail.Width = imageDetail.Width;
                fileDetail.ImageDetail = new ImageDetail { Width = imageDetail.Width, Height = imageDetail.Height };
            }
        }

        await fileDetailRepository.AddAsync(fileDetail).ConfigureAwait(false);
        await fileClassificationService.ClassifyAsync(fileDetail, pageData, result.Tags, ct).ConfigureAwait(false);
    }
}
