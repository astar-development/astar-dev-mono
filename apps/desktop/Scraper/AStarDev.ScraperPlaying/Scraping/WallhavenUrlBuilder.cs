using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Builds the Wallhaven page URLs used to fetch "hot wallpapers", "top wallpapers" and search-category results.</summary>
public static class WallhavenUrlBuilder
{
    /// <summary>Builds the URL for the given page of the "top wallpapers" search.</summary>
    /// <param name="topWallpapersTemplate">The configured URL prefix for the "top wallpapers" search; may be relative to the HTTP client's base address.</param>
    /// <param name="page">The page number to append.</param>
    public static Uri BuildTopWallpapersPageUrl(string topWallpapersTemplate, int page)
        => ToUri(topWallpapersTemplate + page);

    /// <summary>Builds the URL for the given page of the "hot wallpapers" search.</summary>
    /// <param name="hotWallpapersTemplate">The configured URL prefix for the "hot wallpapers" search; may be relative to the HTTP client's base address.</param>
    /// <param name="page">The page number to append.</param>
    public static Uri BuildHotWallpapersPageUrl(string hotWallpapersTemplate, int page)
        => ToUri(hotWallpapersTemplate + page);

    /// <summary>
    /// Builds the URL for the given page of a search category, substituting <paramref name="category"/>'s id
    /// into the <paramref name="searchCategoriesTemplate"/>. The page number is omitted for page 1 to match Wallhaven's URL convention.
    /// </summary>
    /// <param name="searchCategoriesTemplate">The configured search URL template; may be relative to the HTTP client's base address.</param>
    /// <param name="searchCategoriesSuffix">The configured text that follows the category id.</param>
    /// <param name="category">The search category whose id is substituted into the template.</param>
    /// <param name="page">The page number to append, omitted for page 1.</param>
    public static Uri BuildCategoryPageUrl(string searchCategoriesTemplate, string searchCategoriesSuffix, SearchCategoryEntity category, int page)
    {
        var url = searchCategoriesTemplate + category.Id + searchCategoriesSuffix;

        return ToUri(page == 1 ? url : url + page);
    }

    internal static Uri BuildSubscriptionsPageUrl(string subscriptionsTemplate, int page, string searchCategoriesSuffix)
        => ToUri(subscriptionsTemplate + page + searchCategoriesSuffix);

    private static Uri ToUri(string url) => new(url, UriKind.RelativeOrAbsolute);
}
