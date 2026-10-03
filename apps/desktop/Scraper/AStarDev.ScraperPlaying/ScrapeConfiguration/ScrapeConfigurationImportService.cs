using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

public sealed class ScrapeConfigurationImportService(
    IScrapeConfigurationImporter importer,
    IScrapeConfigurationFileReader fileReader) : IScrapeConfigurationImportService
{
    public async Task ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = await fileReader.ReadAsync(filePath, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        _ = (await importer.ImportScrapeConfigurationAsync(document, cancellationToken)).GetOrThrow();
    }
}
