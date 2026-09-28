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
        var result = await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.SearchCategoriesUrl, page, cancellationToken));

        return result.TapError(exception => ReportFailure(progress, "Scraper.SearchCategories.Failed", exception)).Map(_ => UnitFp.Instance);
    }

    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeTopAsync(IProgress<string> progress, IPage page, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.Top.Started"));
        var scrapeConfig = await scrapeConfigurationRepository.GetScrapeConfigurationAsync();
        var result = await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.TopWallpapersUrl, page, cancellationToken));

        return result.TapError(exception => ReportFailure(progress, "Scraper.Top.Failed", exception)).Map(_ => UnitFp.Instance);
    }

    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeSubscribedAsync(IProgress<string> progress, IPage page, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.Subscribed.Started"));
        var scrapeConfig = await scrapeConfigurationRepository.GetScrapeConfigurationAsync();
        var result = await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.SubscribedUrl, page, cancellationToken));

        return result.TapError(exception => ReportFailure(progress, "Scraper.Subscribed.Failed", exception)).Map(_ => UnitFp.Instance);
    }

    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ScrapeAllAsync(IProgress<string> progress, IPage page, CancellationToken cancellationToken)
    {
        progress.Report(localizationService.GetLocal("Scraper.All.Started"));
        var scrapeConfig = await scrapeConfigurationRepository.GetScrapeConfigurationAsync();

        var searchCategoriesResult = await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.SearchCategoriesUrl, page, cancellationToken));
        var topResult = await searchCategoriesResult.BindAsync(async _ => await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.TopWallpapersUrl, page, cancellationToken)));
        var subscribedResult = await topResult.BindAsync(async _ => await scrapeConfig.BindAsync(async config => await pageProcessor.ProcessPageAsync(progress, config.SubscribedUrl, page, cancellationToken)));

        return subscribedResult.TapError(exception => ReportFailure(progress, "Scraper.All.Failed", exception)).Map(_ => UnitFp.Instance);
    }

    private void ReportFailure(IProgress<string> progress, string localizationKey, Exception exception) => progress.Report(localizationService.GetLocal(localizationKey, exception.Message));
}
