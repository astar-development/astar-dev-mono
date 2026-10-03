using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.Utilities;

namespace AStarDev.WallpaperScraper.WallpaperIngestion;

/// <inheritdoc/>
public sealed class WallpaperFileRecorder(TimeProvider timeProvider) : IWallpaperFileRecorder
{
    /// <inheritdoc/>
    public Exceptional<FileEntity> Record(IRepository<FileEntity, FileId> fileRepository, WallpaperFileRequest request)
    {
        var wallpaper = request.Wallpaper;
        var now = timeProvider.GetUtcNow();
        var fileEntity = new FileEntity
        {
            Id = FileId.Empty,
            FileName = request.FileName,
            DirectoryName = DirectoryName.Create(request.Directory),
            FileAccessDetail = NewAccessDetail(now),
            LastUpdated = now,
            FileSize = wallpaper.FileSize,
            FileHandle = FileHandle.Create(wallpaper.Id),
            FileType = wallpaper.FileType,
            IsImage = wallpaper.Path.IsImage,
            ImageDetail = NewImageDetail(wallpaper)
        };

        return fileRepository.Add(fileEntity);
    }

    private static FileAccessDetailEntity NewAccessDetail(DateTimeOffset now)
        => new()
        {
            DetailsLastUpdated = now.UtcDateTime,
            Id = FileAccessDetailId.Empty,
            FileId = FileId.Empty
        };

    private static ImageDetailEntity NewImageDetail(Data wallpaper)
        => new()
        {
            Id = ImageId.Empty,
            FileId = FileId.Empty,
            Width = wallpaper.DimensionX,
            Height = wallpaper.DimensionY,
        };
}
