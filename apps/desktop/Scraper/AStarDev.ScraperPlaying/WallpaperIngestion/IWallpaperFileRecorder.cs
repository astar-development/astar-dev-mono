using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Records a downloaded wallpaper as a file entity.</summary>
public interface IWallpaperFileRecorder
{
    /// <summary>Builds the file entity for the wallpaper in <paramref name="request"/> and adds it to <paramref name="fileRepository"/>.</summary>
    /// <param name="fileRepository">The repository used to store file entities.</param>
    /// <param name="request">The wallpaper, and the file name and directory its image was saved under.</param>
    /// <returns>The added <see cref="FileEntity"/>, or the captured failure from adding it.</returns>
    Exceptional<FileEntity> Record(IRepository<FileEntity, FileId> fileRepository, WallpaperFileRequest request);
}
