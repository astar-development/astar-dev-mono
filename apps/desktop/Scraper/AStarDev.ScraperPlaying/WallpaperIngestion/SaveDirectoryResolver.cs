using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
/// <remarks>Registered as Scoped (one scrape run): the root directories are loaded on first use and reused, rather than reloading the whole scrape configuration for every page set.</remarks>
public sealed class SaveDirectoryResolver(IScrapeConfigurationLookup lookup) : ISaveDirectoryResolver
{
    private const string TopWallpapersDirectorySegment = "top-wallpapers";
    private Option<RootDirectories> rootDirectories = Option.None<RootDirectories>();

    /// <inheritdoc/>
    public async Task<SaveDirectories> ResolveSaveDirectoriesAsync(Option<string> categoryName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directorySegment = categoryName.Match(name => name.ToDirectorySlug(), () => TopWallpapersDirectorySegment);
        var roots = await LoadRootDirectoriesAsync(cancellationToken);

        return new SaveDirectories(roots.Root, roots.FamousRoot, directorySegment);
    }

    private async Task<RootDirectories> LoadRootDirectoriesAsync(CancellationToken cancellationToken)
    {
        if (rootDirectories is Option<RootDirectories>.Some cached) return cached.Value;

        var loaded = (await lookup.TryGetRootDirectoriesAsync(cancellationToken))
            .GetOrThrow()
            .Match(directories => directories, () => throw new InvalidOperationException("No scrape configuration exists."));

        rootDirectories = Option.Some(loaded);

        return loaded;
    }
}
