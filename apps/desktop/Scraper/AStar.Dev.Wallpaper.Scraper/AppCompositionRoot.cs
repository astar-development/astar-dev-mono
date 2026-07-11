using System.Globalization;
using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Classifications;
using AStar.Dev.Wallpaper.Scraper.Pages;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using AStar.Dev.Wallpaper.Scraper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;
using AStar.Dev.Wallpaper.Scraper.Tags;
using AStar.Dev.Wallpaper.Scraper.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;
using Testably.Abstractions;

namespace AStar.Dev.Wallpaper.Scraper;

internal static class AppCompositionRoot
{
    public static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = ApplicationMetadata.ApplicationFolder
        });

        builder.Configuration.AddUserSecrets<App>(optional: true, reloadOnChange: true);
        ConfigureServices(builder.Services, builder.Configuration);

        return builder.Build();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services
            .AddSingleton<LogBroadcaster>()
            .AddSingleton<ImageBroadcaster>()
            .AddSingleton(sp =>
            {
                var broadcaster = sp.GetRequiredService<LogBroadcaster>();
                return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                    .WriteTo.Seq("http://localhost:5341", formatProvider: CultureInfo.InvariantCulture)
                    .WriteTo.Sink(new StatusLogSink(broadcaster.Broadcast), Serilog.Events.LogEventLevel.Information)
                    .Enrich.WithExceptionDetails()
                    .Enrich.FromLogContext()
                    .ReadFrom.Configuration(sp.GetRequiredService<IConfiguration>())
                    .CreateLogger();
            })
            .AddSingleton<ILogger>(sp => sp.GetRequiredService<Serilog.Core.Logger>())
            .AddDbContextFactory<AppDbContext>(options => options.UseSqlite(SqliteConnectionStringProvider.Get(configuration)))
            .AddSingleton<TagsManager>()
            .AddSingleton(sp => sp.GetRequiredService<TagsManager>().TagsToIgnoreCompletely)
            .AddSingleton(sp => sp.GetRequiredService<TagsManager>().TagsTextToIgnore)
            .AddSingleton<ScrapeConfigurationManager>()
            .AddTransient(sp => sp.GetRequiredService<ScrapeConfigurationManager>().Current)
            .AddTransient(sp => sp.GetRequiredService<ScrapeConfigurationManager>().Current.SearchConfiguration)
            .AddTransient<IFileClassificationCategoriesRepository, FileClassificationCategoriesRepository>()
            .AddTransient<IFileDetailRepository, FileDetailRepository>()
            .AddTransient<IDatabaseResetRepository, DatabaseResetRepository>()
            .AddTransient<FileClassificationService>()
            .AddTransient<FileClassificationImportExportService>()
            .AddTransient<IFileClassificationCategoryService, FileClassificationCategoryService>()
            .AddTransient<IDatabaseResetService, DatabaseResetService>()
            .AddTransient<ConfigurationSaver>()
            .AddTransient<DatabaseInitializationService>()
            .AddTransient<ScrapeConfigurationViewModel>()
            .AddViewFactory<ScrapeConfigurationView>()
            .AddViewFactory<ClassificationsView>()
            .AddViewFactory<TagsView>()
            .AddSingleton<IPlaywrightService, PlaywrightService>()
            .AddTransient<SearchWorkflow>()
            .AddTransient<SearchResultsPage>()
            .AddTransient<PagedScrapeRunner>()
            .AddTransient<SubscriptionsImagesListPage>()
            .AddTransient<SubscriptionsWorkflow>()
            .AddTransient<ITopWallpapersPage, TopWallpapersPage>()
            .AddTransient<TopWallpapersWorkflow>()
            .AddTransient<IImportExportService, ImportExportService>()
            .AddTransient<IFileSystem, RealFileSystem>()
            .AddTransient<ScrapeConfigurationService>()
            .AddTransient<ImageDownloader>()
            .AddTransient<ImagePersistence>()
            .AddTransient<ImagePageService>()
            .AddTransient<ImagePage>()
            .AddSingleton<IDirectoryHelper, DirectoryHelper>()
            .AddSingleton<IDelayStrategy, RandomDelayStrategy>()
            .AddTransient<MainWindow>()
            .AddTransient(_ => TimeProvider.System)
            .AddTransient<IImageSaver, ImageSaver>()
            .AddTransient<IImageDimensionReader, ImageDimensionReader>()
            .AddHttpClient<IImageRetriever, ImageRetriever>(client => client.Timeout = TimeSpan.FromMinutes(2));
}
