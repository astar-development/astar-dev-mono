namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

public interface IScrapeConfigurationExportService
{
    Task<bool> ExportAsync(string filePath, ApiKeyExport apiKeys, CancellationToken cancellationToken = default);
}
