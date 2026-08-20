using AStar.Dev.FunctionalParadigm;

namespace AStarDev.WallpaperScraper.Scrapers;

/// <summary>
///   Represents an orchestrator that coordinates the scraping of wallpapers from various sources.
/// </summary>
public sealed class ScrapeOrchestrator : IScrapeOrchestrator
{
    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeSearchCategoriesAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException("ScrapeSearchCategoriesAsync is not implemented yet.");

    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeTopAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException("ScrapeTopAsync is not implemented yet.");

    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeSubscribedAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException("ScrapeSubscribedAsync is not implemented yet.");

    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeAllAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException("ScrapeAllAsync is not implemented yet.");
}
