namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Persists Top Wallpapers and Subscriptions paging progress to the current <see cref="AStar.Dev.Infrastructure.AppDb.Entities.SearchConfigurationEntity" />
///     row, so a scrape can resume near where it left off.
/// </summary>
public interface ISearchConfigurationProgressWriter
{
    /// <summary>Persists the current Top Wallpapers paging position.</summary>
    /// <param name="startingPageNumber">The page number to resume from on the next run.</param>
    /// <param name="totalPages">The total number of Top Wallpapers pages currently available.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the write.</param>
    Task WriteTopWallpapersProgressAsync(int startingPageNumber, int totalPages, CancellationToken cancellationToken);

    /// <summary>Persists the current Subscriptions paging position.</summary>
    /// <param name="startingPageNumber">The page number to resume from on the next run.</param>
    /// <param name="totalPages">The total number of Subscriptions pages currently available.</param>
    /// <param name="cancellationToken">A token used to observe cancellation of the write.</param>
    Task WriteSubscriptionsProgressAsync(int startingPageNumber, int totalPages, CancellationToken cancellationToken);
}
