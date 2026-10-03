namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

public interface IScrapeConfigurationImportService
{
    Task ImportAsync(string filePath, CancellationToken cancellationToken = default);
}
