namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>
/// Holds the <see cref="TagFlagCache"/> for one scrape run (Scoped) so <see cref="TagFetcher"/> and <see cref="TagLinker"/> share it.
/// The first fetch always completes before any other database work starts, so the load never overlaps another use of the database context.
/// The cache and loaded flag are published together as one immutable state, so a concurrent reader never sees them disagree.
/// </summary>
public sealed class TagFlagStore
{
    private volatile State state = State.Unloaded;

    /// <summary>Gets the loaded cache, or <see cref="TagFlagCache.Empty"/> until <see cref="Load"/> is called.</summary>
    public TagFlagCache Cache => state.Cache;

    /// <summary>Gets a value indicating whether the cache has been loaded.</summary>
    public bool IsLoaded => state.IsLoaded;

    /// <summary>Stores the cache loaded for this run.</summary>
    /// <param name="cache">The loaded cache.</param>
    public void Load(TagFlagCache cache) => state = new State(cache, true);

    private sealed record State(TagFlagCache Cache, bool IsLoaded)
    {
        public static State Unloaded { get; } = new(TagFlagCache.Empty, false);
    }
}
