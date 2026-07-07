using AStar.Dev.Guard.Clauses;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

/// <summary>Factory methods for creating instances of <see cref="SearchProgress" />.</summary>
public static class SearchProgressFactory
{
    /// <summary>Creates a <see cref="SearchProgress" />.</summary>
    public static SearchProgress Create(SearchConfiguration searchConfiguration, ScrapeDirectories scrapeDirectories)
    {
        GuardAgainst.Null(searchConfiguration);
        GuardAgainst.Null(scrapeDirectories);

        return new(searchConfiguration, scrapeDirectories);
    }
}
