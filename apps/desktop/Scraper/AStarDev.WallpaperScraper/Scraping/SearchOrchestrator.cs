using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.Utilities;
using Microsoft.Extensions.Logging;

namespace AStarDev.WallpaperScraper.Scraping;

/// <inheritdoc/>
public sealed class SearchOrchestrator(IPagesProcessor pagesProcessor, ScrapeLimits limits, ILogger<SearchOrchestrator> logger) : ISearchOrchestrator
{
    /// <summary>The hot wallpapers are saved and labelled like a category of this name, so they land in the "hot-wallpapers" directory.</summary>
    private const string HotWallpapersName = "Hot Wallpapers";

    /// <inheritdoc/>
    public async Task RunSearchesAsync(ScrapeConfigurationEntity configuration, ScrapeSelection selection, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var target = new ScrapeTarget(new WallhavenConnection(configuration.UserConfiguration.ApiKey, configuration.BaseUrl), [.. configuration.SearchConfiguration.PersonCategories.Select(category => category.Name)]);

        if (selection.HotWallpapers)
        {
            progress.Report("Fetching hot wallpapers.");
            await RunSearchAsync(HotRequest(configuration, target), progress, cancellationToken);
        }

        if (selection.TopWallpapers)
        {
            progress.Report("Fetching top wallpapers.");
            await RunSearchAsync(TopRequest(configuration, target), progress, cancellationToken);
        }

        if (!selection.Categories) return;

        progress.Report("Fetching categories.");
        foreach (var category in configuration.SearchConfiguration.SearchCategories.Where(category => category.IncludeInSearch).OrderByDescending(category => category.IsFamous).ThenByDescending(category => category.IsInternet).ThenBy(category => category.Name, StringComparer.OrdinalIgnoreCase).Take(limits.MaximumSearchCategories))
        {
            await RunSearchAsync(CategoryRequest(configuration, category, target), progress, cancellationToken);
        }
    }

    /// <summary>Runs one search. A page that cannot be fetched ends only this search: it is reported once and the next search still runs, and its progress stays withheld so it resumes next scrape. Anything else, including a cancellation, propagates.</summary>
    private async Task RunSearchAsync(PageScrapeRequest request, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            await pagesProcessor.FetchAndProcessPagesAsync(request, progress, cancellationToken);
        }
        catch (PageFetchException exception) when (!cancellationToken.IsCancellationRequested)
        {
            progress.Report($"The {request.Label.LogLabel} search failed: {exception.Failure.ToMessageChain()}");
            LogMessage.Error(logger, $"Search {request.Label.LogLabel} failed", exception.Failure);
        }
    }

    private static PageScrapeRequest HotRequest(ScrapeConfigurationEntity configuration, ScrapeTarget target)
        => new(new ScrapeLabel("hot wallpapers", Option.Some(HotWallpapersName)), Option.None<SearchCategoryProgress>(), new PageHooks(_ => { }, page => WallhavenUrlBuilder.BuildHotWallpapersPageUrl(configuration.HotWallpapers, page)), target) { Batching = PageBatchingFactory.CreateUnbatched() };

    private static PageScrapeRequest TopRequest(ScrapeConfigurationEntity configuration, ScrapeTarget target)
        => new(new ScrapeLabel("top wallpapers", Option.None<string>()), Option.None<SearchCategoryProgress>(), new PageHooks(_ => { }, page => WallhavenUrlBuilder.BuildTopWallpapersPageUrl(configuration.TopWallpapers, page)), target) { Batching = PageBatchingFactory.CreateUnbatched() };

    private static PageScrapeRequest CategoryRequest(ScrapeConfigurationEntity configuration, SearchCategoryEntity category, ScrapeTarget target)
        => new(new ScrapeLabel($"search category {category.Name}", Option.Some(category.Name)), Option.Some(new SearchCategoryProgress(category.LastKnownImageCount, category.LastPageVisited, category.TotalPages)), new PageHooks(RecordProgress(category), page => WallhavenUrlBuilder.BuildCategoryPageUrl(configuration.SearchStringPrefix, configuration.SearchStringSuffix, category, page)), target);

    private static Action<SearchCategoryProgress> RecordProgress(SearchCategoryEntity category)
        => scraped => category.RecordScrapeProgress(scraped.LastKnownImageCount, scraped.LastPageVisited, scraped.TotalPages);
}
