namespace AStarDev.WallpaperScraper.Scraping;

/// <summary>Interface for the scrape service that handles running the scraper.</summary>
public interface IScrapeService
{
    /// <summary>Runs the scraper asynchronously, reporting progress through the provided progress reporter.</summary>
    /// <param name="selection">Which of the scrapes to run.</param>
    /// <param name="progress">The progress reporter to report the scraping progress.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunScraperAsync(ScrapeSelection selection, IProgress<string> progress);
}

