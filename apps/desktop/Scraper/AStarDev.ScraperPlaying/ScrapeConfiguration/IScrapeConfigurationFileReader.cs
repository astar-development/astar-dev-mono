namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

public interface IScrapeConfigurationFileReader
{
    Task<ScrapeConfigurationImportDocument> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}
