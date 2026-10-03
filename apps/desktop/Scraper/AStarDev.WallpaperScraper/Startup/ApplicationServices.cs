using AStarDev.WallpaperScraper.Operations;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AStarDev.WallpaperScraper.Downloads;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.Scoping;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Tags;
using AStarDev.WallpaperScraper.UI;
using AStarDev.WallpaperScraper.WallpaperIngestion;

namespace AStarDev.WallpaperScraper.Startup;

/// <summary>
/// Registers the application's services with the dependency injection container, one domain at a time. A registration's lifetime must never be
/// broader than any of its dependencies' (no singleton depending on a scoped or transient service), which avoids captive dependencies.
/// </summary>
public static class ApplicationServices
{
    /// <summary>Registers every domain's services with the dependency injection container.</summary>
    /// <param name="services">The service collection to register the application's services with.</param>
    /// <param name="configuration">The application configuration used to bind the options sections.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddCoreServices()
            .AddScrapeConfigurationServices()
            .AddScrapingServices(configuration)
            .AddWallpaperIngestionServices()
            .AddTagsAndDownloadsServices()
            .AddUiServices();

    /// <summary>Registers the application directories, scoped-service runner, clock and database initialisation.</summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
        => services
            .AddSingleton<IApplicationDirectories, ApplicationDirectories>()
            .AddSingleton<IScopedRunner, ScopedRunner>()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IDatabaseInitialization, DatabaseInitialization>();

    /// <summary>Registers reading, importing, exporting, browsing and editing the scrape configuration.</summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddScrapeConfigurationServices(this IServiceCollection services)
        => services
            .AddSingleton<IRootDirectoryCheck, RootDirectoryCheck>()
            .AddSingleton<IScrapeConfigurationFileReader, ScrapeConfigurationFileReader>()
            .AddSingleton<IScrapeConfigurationImportService, ScrapeConfigurationImportService>()
            .AddSingleton<IScrapeConfigurationImporter, ScrapeConfigurationImporter>()
            .AddSingleton<IScrapeConfigurationFileWriter, ScrapeConfigurationFileWriter>()
            .AddSingleton<IScrapeConfigurationExportService, ScrapeConfigurationExportService>()
            .AddSingleton<IScrapeConfigurationExporter, ScrapeConfigurationExporter>()
            .AddSingleton<IScrapeConfigurationCatalogue, ScrapeConfigurationCatalogue>()
            .AddSingleton<IScrapeConfigurationUpdater, ScrapeConfigurationUpdater>()
            .AddSingleton<ConfigurationEditSaver>();

    /// <summary>Registers the Wallhaven client, its rate limiting and the page-by-page scraping pipeline.</summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <param name="configuration">The application configuration used to bind the scrape limits.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddScrapingServices(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddSingleton<IScrapeService, ScrapeService>()
            .AddSingleton(ScrapeLimits.From(configuration))
            .AddSingleton<IWallhavenClientFactory, WallhavenClientFactory>()
            .AddSingleton<RateLimiter>(_ => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = ApplicationConstants.WallhavenRequestsPerWindow,
                Window = ApplicationConstants.WallhavenRateLimitWindow,
                SegmentsPerWindow = 12,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }))
            .AddSingleton<IRateLimitBackoffNotifier, RateLimitBackoffNotifier>()
            .AddTransient<WallhavenRateLimitingHandler>()
            .AddTransient<WallhavenApiKeyHandler>()
            .AddScoped<IWallhavenPageFetcher, WallhavenPageFetcher>()
            .AddSingleton<ScrapeResumePolicy>()
            .AddScoped<IWallpaperIngestionContextFactory, WallpaperIngestionContextFactory>()
            .AddScoped<PageIngestionStep>()
            .AddScoped<IPagesProcessor, PagesProcessor>()
            .AddScoped<ISearchOrchestrator, SearchOrchestrator>()
            .AddScoped<IJsonResponseProcessor, JsonResponseProcessor>()
            .AddHttpClient(ApplicationConstants.WallhavenHttpClientName, client =>
            {
                client.Timeout = ScrapeTimeouts.Default.Client;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddHttpMessageHandler<WallhavenApiKeyHandler>().AddHttpMessageHandler<WallhavenRateLimitingHandler>().Services;

    /// <summary>Registers downloading, recording and previewing wallpapers.</summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddWallpaperIngestionServices(this IServiceCollection services)
        => services
            .AddSingleton<IImageDownloadNotifier, ImageDownloadNotifier>()
            .AddSingleton<IDownloadedImageDecoder, DownloadedImageDecoder>()
            .AddSingleton(DownloadPacing.Default)
            .AddSingleton(ScrapeTimeouts.Default)
            .AddScoped<IImageDownloader, ImageDownloader>()
            .AddScoped<IWallpaperFileRecorder, WallpaperFileRecorder>()
            .AddScoped<WallpaperSaver>()
            .AddScoped<WallpaperFiler>()
            .AddScoped<IgnoredWallpaperRecorder>()
            .AddScoped<INewWallpaperIngestor, NewWallpaperIngestor>()
            .AddScoped<ISaveDirectoryResolver, SaveDirectoryResolver>()
            .AddScoped<IWallpaperIngestionService, WallpaperIngestionService>();

    /// <summary>Registers the tag catalogue, tag processing and clearing of downloads.</summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddTagsAndDownloadsServices(this IServiceCollection services)
        => services
            .AddSingleton<ITagCatalogue, TagCatalogue>()
            .AddSingleton<IDownloadsClearer, DownloadsClearer>()
            .AddScoped<TagFlagStore>()
            .AddScoped<ITagFetcher, TagFetcher>()
            .AddScoped<ITagLinker, TagLinker>();

    /// <summary>Registers the main window and the collaborators that hold its logic.</summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddUiServices(this IServiceCollection services)
        => services
            .AddSingleton<ImageDisplayCoordinator>()
            .AddSingleton<OperationCoordinator>()
            .AddSingleton<StatusReporter>()
            .AddSingleton<UserOperationRunner>()
            .AddSingleton<ConfigurationEditorWindowFactory>()
            .AddSingleton<ConfigurationBrowser>()
            .AddSingleton<TagsBrowser>()
            .AddSingleton<ClearDownloadsRunner>()
            .AddSingleton<ScrapeRunner>()
            .AddSingleton<ApplicationReadiness>()
            .AddSingleton<ConfigurationTransfer>()
            .AddSingleton<ConfigurationActions>()
            .AddSingleton<TagActions>()
            .AddSingleton<ScrapeActions>()
            .AddSingleton<MainWindowActions>()
            .AddSingleton<MainWindow>();
}
