using AStar.Dev.Guard.Clauses;
using AStar.Dev.Wallpaper.Scraper.Repositories;

namespace AStar.Dev.Wallpaper.Scraper.Models;

/// <summary>Factory methods for creating instances of the <see cref="ImagePageOutcome" /> discriminated union.</summary>
public static class ImagePageOutcomeFactory
{
    /// <summary>Creates a <see cref="ScrapedImage" /> outcome.</summary>
    public static ScrapedImage CreateScrapedImage(string imageUrl, IReadOnlyList<string> directorySegments, string filePrefix, IReadOnlyList<TagData> tags, IReadOnlyList<TagData> rawTags)
    {
        GuardAgainst.Null(imageUrl);
        GuardAgainst.Null(directorySegments);
        GuardAgainst.Null(filePrefix);
        GuardAgainst.Null(tags);
        GuardAgainst.Null(rawTags);

        return new(imageUrl, directorySegments, filePrefix, tags, rawTags);
    }

    /// <summary>Creates a <see cref="SkippedImage" /> outcome.</summary>
    public static SkippedImage CreateSkippedImage(IReadOnlyList<TagData> tags, IReadOnlyList<TagData> rawTags)
    {
        GuardAgainst.Null(tags);
        GuardAgainst.Null(rawTags);

        return new(tags, rawTags);
    }
}
