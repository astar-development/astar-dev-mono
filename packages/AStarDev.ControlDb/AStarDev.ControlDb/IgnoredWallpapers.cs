using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Implementation of the <see cref="IIgnoredWallpapers"/> interface, tracking the handles on the shared database context.</summary>
/// <param name="context">The database context the page's other changes are saved through.</param>
public class IgnoredWallpapers(ControlDbContext context) : IIgnoredWallpapers
{
    // The store is scoped with the context, so this holds exactly the handles recorded on it; a set lookup replaces scanning every tracked row on each record.
    private readonly HashSet<string> recorded = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Exceptional<Unit> Record(FileHandle fileHandle)
        => Try.Run(() =>
        {
            if (recorded.Add(fileHandle.Value)) _ = context.IgnoredWallpapers.Add(new IgnoredWallpaperEntity { FileHandle = fileHandle });

            return Unit.Instance;
        });

    /// <inheritdoc/>
    public Task<Exceptional<int>> ForgetAllAsync(CancellationToken cancellationToken = default)
        => Try.RunAsync(() => context.IgnoredWallpapers.ExecuteDeleteAsync(cancellationToken));
}
