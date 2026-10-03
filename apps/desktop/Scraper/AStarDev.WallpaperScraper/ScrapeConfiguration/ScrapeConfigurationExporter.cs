using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scoping;

namespace AStarDev.WallpaperScraper.ScrapeConfiguration;

/// <summary>Reads the scrape configuration settings aggregate for export.</summary>
/// <param name="scopedRunner">Runs each export in its own scope, with its own unit of work.</param>
public sealed class ScrapeConfigurationExporter(IScopedRunner scopedRunner) : IScrapeConfigurationExporter
{
    /// <inheritdoc/>
    public async Task<Exceptional<Option<ScrapeConfigurationImportDocument>>> ExportScrapeConfigurationAsync(ApiKeyExport apiKeys, CancellationToken cancellationToken = default)
        =>
        await scopedRunner.RunAsync<IUnitOfWork, Exceptional<Option<ScrapeConfigurationImportDocument>>>(unitOfWork => Try.RunAsync(async () =>
        {
            var dbContext = unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();
            var current = (await dbContext.TryGetFirstAsync(cancellationToken)).GetOrThrow();

            return current.Match(entity => (Option<ScrapeConfigurationImportDocument>)entity.ToImportDocument(apiKeys), () => Option<ScrapeConfigurationImportDocument>.None.Instance);
        }));
}
