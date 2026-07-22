using AStar.Dev.Wallpaper.Scraper.Maintenance;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Wallpaper.Scraper;

/// <summary>Registers the wallpaper scraping, classification, and download pipeline services with the dependency injection container.</summary>
public static class ScrapingServiceCollectionExtensions
{
    /// <summary>Registers the scraping, category, thumbnail, and download pipeline services.</summary>
    /// <param name="services">The service collection to register the scraping services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddScrapingServices(this IServiceCollection services)
    {
        services.AddSingleton<IPlaywrightService, PlaywrightService>();
        services.AddSingleton<ISearchCategoryWriter, SearchCategoryWriter>();
        services.AddSingleton<ISearchCategoryReader, SearchCategoryReader>();
        services.AddSingleton<IScrapeContextReader, ScrapeContextReader>();
        services.AddSingleton<IWallpaperCountReader, WallpaperCountReader>();
        services.AddSingleton<IWallpaperHrefCollector, WallpaperHrefCollector>();
        services.AddSingleton<ITagReader, TagReader>();
        services.AddSingleton<IWallpaperImageLocator, WallpaperImageLocator>();
        services.AddSingleton<IRawWallpaperImageDownloader, WallpaperImageDownloader>();
        services.AddSingleton<IWallpaperThumbnailGenerator, WallpaperThumbnailGenerator>();
        services.AddSingleton<WallpaperThumbnailBroadcaster>();
        services.AddSingleton<IWallpaperThumbnailPublisher>(serviceProvider => serviceProvider.GetRequiredService<WallpaperThumbnailBroadcaster>());
        services.AddSingleton<IWallpaperThumbnailFeed>(serviceProvider => serviceProvider.GetRequiredService<WallpaperThumbnailBroadcaster>());
        services.AddSingleton<IWallpaperImageDownloader, ThumbnailPublishingWallpaperImageDownloader>();
        services.AddSingleton<IImageDimensionsReader, SkiaImageDimensionsReader>();
        services.AddSingleton<IWallpaperFileStore, WallpaperFileStore>();
        services.AddSingleton<IWallpaperCategoryRegistrar, WallpaperCategoryRegistrar>();
        services.AddSingleton<IWallpaperFileClassificationRepository, WallpaperFileClassificationRepository>();
        services.AddSingleton<IScrapeAction, SearchCategoryScrapeAction>();
        services.AddSingleton<IDatabaseResetService, DatabaseResetService>();

        return services;
    }
}
