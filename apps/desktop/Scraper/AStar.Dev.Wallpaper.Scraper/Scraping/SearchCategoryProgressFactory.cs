namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Factory for <see cref="SearchCategoryProgress"/>.
/// </summary>
public static class SearchCategoryProgressFactory
{
    /// <summary>
    ///     Creates a <see cref="SearchCategoryProgress"/> from values read from the database. Negative
    ///     values (which stored data should never contain, but a future migration or manual edit could
    ///     otherwise produce) are normalized to zero rather than propagated.
    /// </summary>
    /// <param name="lastKnownImageCount">The stored image count for the category.</param>
    /// <param name="lastPageVisited">The stored last page visited for the category.</param>
    public static SearchCategoryProgress Create(int lastKnownImageCount, int lastPageVisited) =>
        new(Math.Max(lastKnownImageCount, 0), Math.Max(lastPageVisited, 0));
}
