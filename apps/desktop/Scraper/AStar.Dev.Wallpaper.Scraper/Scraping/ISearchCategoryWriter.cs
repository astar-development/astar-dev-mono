using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Persists a search category's scrape progress to the database.
/// </summary>
public interface ISearchCategoryWriter
{
    /// <summary>
    ///     Updates the existing <c>SearchCategoryEntity</c> matching <paramref name="searchCategory" />'s name with its
    ///     famous/internet flags and latest scrape progress. Search categories are user-managed, so no new category is
    ///     ever created; a missing match is reported as a failure.
    /// </summary>
    /// <param name="searchCategory">The scrape progress to persist.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the write.</param>
    Task<Result<Unit, string>> WriteAsync(SearchCategoryDto searchCategory, CancellationToken cancellationToken);
}
