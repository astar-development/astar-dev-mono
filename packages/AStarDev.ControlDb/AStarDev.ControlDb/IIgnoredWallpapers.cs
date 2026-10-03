using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>Remembers the wallpapers that were ignored because of a tag flagged to ignore images, so they are not fetched again on every scrape. <see cref="IFilesQuery.GetExistingHandlesAsync"/> reports them as existing.</summary>
public interface IIgnoredWallpapers
{
    /// <summary>Remembers that the wallpaper is ignored. The handle is stored when the unit of work is next saved, together with the rest of the page; a handle already remembered is not added again.</summary>
    /// <param name="fileHandle">The handle of the ignored wallpaper.</param>
    /// <returns>An exceptional result that is a failure when the handle could not be tracked.</returns>
    Exceptional<Unit> Record(FileHandle fileHandle);

    /// <summary>Forgets every ignored wallpaper immediately, so each is evaluated again on the next scrape. Used when a tag stops being flagged to ignore, because a remembered wallpaper may no longer have a tag that is ignored.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing the number of ignored wallpapers forgotten.</returns>
    Task<Exceptional<int>> ForgetAllAsync(CancellationToken cancellationToken = default);
}
