namespace AStar.Dev.Wallpaper.Scraper.Maintenance;

/// <summary>Resets the scraped data held in the database and, separately, the downloaded files on disk.</summary>
public interface IDatabaseResetService
{
    /// <summary>Clears the file, file access, and image detail tables, and resets every search category's scrape progress.</summary>
    /// <param name="cancellationToken">Used to cancel the operation.</param>
    Task ResetDatabaseAsync(CancellationToken cancellationToken);

    /// <summary>Deletes the configured root directory, and everything beneath it, from disk.</summary>
    /// <param name="cancellationToken">Used to cancel the operation.</param>
    Task RemoveDownloadedFilesAsync(CancellationToken cancellationToken);
}
