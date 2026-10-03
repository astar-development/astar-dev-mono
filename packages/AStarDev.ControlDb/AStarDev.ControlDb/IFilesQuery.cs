using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>Provides methods for querying files in the database.</summary>
public interface IFilesQuery
{
    /// <summary>Gets the handles among those supplied that identify a stored file or a wallpaper remembered as ignored (see <see cref="IIgnoredWallpapers"/>), so neither is fetched again. Handles are matched using the database collation, so the returned handles may differ in case from those supplied.</summary>
    /// <param name="fileHandles">The handles of the files to search for. A file's handle is stable across renames, unlike its file name.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing the stored handles of the files that exist and of the wallpapers remembered as ignored.</returns>
    Task<Exceptional<IReadOnlyList<FileHandle>>> GetExistingHandlesAsync(IReadOnlyCollection<FileHandle> fileHandles, CancellationToken cancellationToken = default);
}
