using AStar.Dev.Guard.Clauses;
using AStar.Dev.Wallpaper.Scraper.Repositories;

namespace AStar.Dev.Wallpaper.Scraper.Support;

/// <summary>Factory methods for creating instances of the <see cref="TagOutcome" /> discriminated union.</summary>
public static class TagOutcomeFactory
{
    /// <summary>Creates a <see cref="SkipImage" /> outcome.</summary>
    public static SkipImage CreateSkipImage(IReadOnlyList<TagData> tags)
    {
        GuardAgainst.Null(tags);

        return new(tags);
    }

    /// <summary>Creates an <see cref="Accept" /> outcome.</summary>
    public static Accept CreateAccept(string filePrefix, IReadOnlyList<string> directorySegments, IReadOnlyList<TagData> tags)
    {
        GuardAgainst.Null(filePrefix);
        GuardAgainst.Null(directorySegments);
        GuardAgainst.Null(tags);

        return new(filePrefix, directorySegments, tags);
    }
}
