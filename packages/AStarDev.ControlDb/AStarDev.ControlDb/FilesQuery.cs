using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Implementation of the IFilesQuery interface for querying files in the database.</summary>
/// <param name="context">The database context used for querying files.</param>
public class FilesQuery(ControlDbContext context) : IFilesQuery
{
    /// <inheritdoc/>
    public Task<Exceptional<IReadOnlyList<FileHandle>>> GetExistingHandlesAsync(IReadOnlyCollection<FileHandle> fileHandles, CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyList<FileHandle>>(async () =>
            {
                var stored = await context.Files
                                          .AsNoTracking()
                                          .Where(file => fileHandles.Contains(file.FileHandle))
                                          .Select(file => file.FileHandle)
                                          .ToListAsync(cancellationToken)
                                          .ConfigureAwait(false);
                var ignored = await context.IgnoredWallpapers
                                           .AsNoTracking()
                                           .Where(ignoredWallpaper => fileHandles.Contains(ignoredWallpaper.FileHandle))
                                           .Select(ignoredWallpaper => ignoredWallpaper.FileHandle)
                                           .ToListAsync(cancellationToken)
                                           .ConfigureAwait(false);

                return [.. stored, .. ignored];
            });
}
