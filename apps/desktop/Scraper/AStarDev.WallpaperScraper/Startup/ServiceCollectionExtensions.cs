using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.WallpaperScraper.Home;
using AStarDev.WallpaperScraper.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using AStarDev.ControlDb;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Scrapers;
using AStarDev.SourceGenerators.OptionsBindingGeneration;
using AStarDev.Utilities;

namespace AStarDev.WallpaperScraper.Startup;

/// <summary>Registers the application's services with the dependency injection container.</summary>
public static class ApplicationServicesExtensions
{
    /// <summary>Registers configuration, infrastructure, scraping, and UI services with the dependency injection container.</summary>
    /// <param name="services">The service collection to register the application's services with.</param>
    /// <param name="configuration">The application configuration used to bind the options sections.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration) =>
        services.AddAnnotatedServices()
            .AddAutoRegisteredOptions(configuration)
            .AddLocalizationServices()
            .AddSingleton<IApplicationDirectories, ApplicationDirectories>()
            .AddSingleton<StartupDiagnostics>()
            .AddSingleton<IUpdateDialogTextProvider, PlainUpdateDialogTextProvider>()
            .AddSingleton<IPlaywrightService, PlaywrightService>()
            .AddSingleton<IScrapeOrchestrator, ScrapeOrchestrator>()
            .AddSingleton<MainWindowViewModel>()
            .AddSingleton<MainWindow>()
            .AddDbContextFactory<ControlDbContext>((serviceProvider, options) =>
                options.UseSqlite($"Data Source={ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory().CombinePath(Path.DirectorySeparatorChar.ToString()).CombinePath("data").CombinePath(Path.DirectorySeparatorChar.ToString()).CombinePath("astar-control.db")}"))
            .AddVelopackUpdates(configuration)
            .AddVelopackUpdateNotifications();
}
