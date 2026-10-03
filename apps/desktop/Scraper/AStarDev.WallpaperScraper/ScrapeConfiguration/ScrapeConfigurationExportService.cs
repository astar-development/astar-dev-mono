using AStarDev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

public sealed class ScrapeConfigurationExportService(
    IScrapeConfigurationExporter exporter,
    IScrapeConfigurationFileWriter fileWriter) : IScrapeConfigurationExportService
{
    public async Task<bool> ExportAsync(string filePath, ApiKeyExport apiKeys, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = (await exporter.ExportScrapeConfigurationAsync(apiKeys, cancellationToken)).GetOrThrow();

        return await result.MatchAsync(
            async document =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await fileWriter.WriteAsync(document, filePath, cancellationToken);

                return true;
            },
            () => false);
    }
}
