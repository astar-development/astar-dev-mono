using AStar.Dev.Guard.Clauses;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

/// <summary>Factory methods for creating instances of <see cref="PageInfo" />.</summary>
public static class PageInfoFactory
{
    /// <summary>Creates a <see cref="PageInfo" />.</summary>
    public static PageInfo Create(int pageCount, int imageCount, string subDirectoryName)
    {
        GuardAgainst.Null(subDirectoryName);

        return new(pageCount, imageCount, subDirectoryName);
    }
}
