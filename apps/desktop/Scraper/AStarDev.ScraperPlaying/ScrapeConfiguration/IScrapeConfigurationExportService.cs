namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

public interface IScrapeConfigurationExportService
{
    Task<bool> ExportAsync(string filePath, ApiKeyExport apiKeys, CancellationToken cancellationToken = default);
}
