using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using AStar.Dev.Wallpaper.Scraper.Support;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public sealed class ImagePersistence(IImageSaver imageSaver, IImageDimensionReader imageDimensionReader, IFileDetailRepository fileDetailRepository, ImageBroadcaster imageBroadcaster, Logger logger)
{
    private const int LoggedPathTailLength = 50;

    public async Task<Result<FileDetailEntity, ScrapeError>> SaveAndPersistAsync(byte[] image, string imageNameWithPath, string filename, DirectoryName directoryName, CancellationToken cancellationToken)
    {
        logger.Information("About to save {filename} to ...{imageNameWithPath} as we don't appear to have it.", filename, TruncatedForLogging(imageNameWithPath));

        return await imageSaver.SaveAsync(image, imageNameWithPath)
            .TapAsync(_ => imageBroadcaster.Broadcast(imageNameWithPath))
            .BindAsync(_ => PersistFileDetailAsync(image, imageNameWithPath, filename, directoryName, cancellationToken))
            .ConfigureAwait(false);
    }

    private Task<Result<FileDetailEntity, ScrapeError>> PersistFileDetailAsync(byte[] image, string imageNameWithPath, string filename, DirectoryName directoryName, CancellationToken cancellationToken)
    {
        var fileDetail = new FileDetailEntity
        {
            DirectoryName = directoryName,
            FileName = new FileName(filename),
            FileSize = image.Length,
            IsImage = filename.IsImage()
        };

        ApplyImageDimensions(fileDetail, image, imageNameWithPath);

        return fileDetailRepository.AddAsync(fileDetail, cancellationToken)
            .BindAsync(_ => Task.FromResult(Result.Success<FileDetailEntity, ScrapeError>(fileDetail)));
    }

    private void ApplyImageDimensions(FileDetailEntity fileDetail, byte[] image, string imageNameWithPath)
        => imageDimensionReader.Read(image, imageNameWithPath)
            .Tap(
                dimensions => fileDetail.ImageDetail = new ImageDetailEntity { Width = dimensions.Width, Height = dimensions.Height },
                error => logger.Warning("Could not read image dimensions for {imageNameWithPath}: {Message}", TruncatedForLogging(imageNameWithPath), error.Message));

    private static string TruncatedForLogging(string imageNameWithPath)
        => imageNameWithPath.Length > LoggedPathTailLength ? imageNameWithPath[^LoggedPathTailLength..] : imageNameWithPath;
}
