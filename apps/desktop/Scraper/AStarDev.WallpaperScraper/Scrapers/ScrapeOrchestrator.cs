using AStar.Dev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Localization;
using AStarDev.WallpaperScraper.Services;
using Microsoft.Playwright;

namespace AStarDev.WallpaperScraper.Scrapers;

/// <summary>
///   Represents an orchestrator that coordinates the scraping of wallpapers from various sources.
/// </summary>
/// <param name="localizationService">Provides the localised status message templates reported via <see cref="IProgress{T}" />.</param>
public sealed class ScrapeOrchestrator(ILocalizationService localizationService, IPageProcessor pageProcessor, IScrapeConfigurationRepository scrapeConfigurationRepository) : IScrapeOrchestrator
{
    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeSearchCategoriesAsync(IProgress<string> progress, IPage page, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.SearchCategories.Started"));
        var scrapeConfig = await scrapeConfigurationRepository.GetScrapeConfigurationAsync();
        await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.SearchCategoriesUrl, page, cancellationToken));

        return UnitFp.Instance;
    }

    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeTopAsync(IProgress<string> progress, IPage page, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.Top.Started"));

        var scrapeConfig = await scrapeConfigurationRepository.GetScrapeConfigurationAsync();
        await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.TopWallpapersUrl, page, cancellationToken));

        return UnitFp.Instance;
    }

    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeSubscribedAsync(IProgress<string> progress, IPage page, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.Subscribed.Started"));

        var scrapeConfig = await scrapeConfigurationRepository.GetScrapeConfigurationAsync();
        await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.SubscribedUrl, page, cancellationToken));

        return UnitFp.Instance;
    }

    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeAllAsync(IProgress<string> progress, IPage page, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.All.Started"));

        var scrapeConfig = await scrapeConfigurationRepository.GetScrapeConfigurationAsync();
        await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.SearchCategoriesUrl, page, cancellationToken));
        await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.TopWallpapersUrl, page, cancellationToken));
        await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.SubscribedUrl, page, cancellationToken));

        return UnitFp.Instance;
    }
}
