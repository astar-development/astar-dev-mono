namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

public interface IScrapeConfigurationFileWriter
{
    Task WriteAsync(ScrapeConfigurationImportDocument document, string filePath, CancellationToken cancellationToken = default);
}
