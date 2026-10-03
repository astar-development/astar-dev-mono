namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

public interface IScrapeConfigurationFileReader
{
    Task<ScrapeConfigurationImportDocument> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}
