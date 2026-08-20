using AStar.Dev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Localization;

namespace AStarDev.WallpaperScraper.Scrapers;

/// <summary>
///   Represents an orchestrator that coordinates the scraping of wallpapers from various sources.
/// </summary>
/// <param name="localizationService">Provides the localised status message templates reported via <see cref="IProgress{T}" />.</param>
public sealed class ScrapeOrchestrator(ILocalizationService localizationService) : IScrapeOrchestrator
{
    /// <inheritdoc/>
    public Task<Exceptional<UnitFp>> ScrapeSearchCategoriesAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.SearchCategories.Started"));

        throw new NotImplementedException("ScrapeSearchCategoriesAsync is not implemented yet.");
    }

    /// <inheritdoc/>
    public Task<Exceptional<UnitFp>> ScrapeTopAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.Top.Started"));

        throw new NotImplementedException("ScrapeTopAsync is not implemented yet.");
    }

    /// <inheritdoc/>
    public Task<Exceptional<UnitFp>> ScrapeSubscribedAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.Subscribed.Started"));

        throw new NotImplementedException("ScrapeSubscribedAsync is not implemented yet.");
    }

    /// <inheritdoc/>
    public Task<Exceptional<UnitFp>> ScrapeAllAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.All.Started"));

        throw new NotImplementedException("ScrapeAllAsync is not implemented yet.");
    }
}
