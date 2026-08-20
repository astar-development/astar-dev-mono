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
using Testably.Abstractions;
using System.IO.Abstractions;

namespace AStarDev.WallpaperScraper.Startup;

/// <summary>Registers the application's services with the dependency injection container.</summary>
public static class ApplicationServicesExtensions
{
    /// <summary>Registers configuration, infrastructure, scraping, and UI services with the dependency injection container.</summary>
    /// <param name="services">The service collection to register the application's services with.</param>
    /// <param name="configuration">The application configuration used to bind the options sections.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration) =>
        services.AddSingleton<IApplicationDirectories, ApplicationDirectories>()
            .AddSingleton<IUpdateDialogTextProvider, PlainUpdateDialogTextProvider>()
            .AddSingleton<IPlaywrightService, PlaywrightService>()
            .AddSingleton<IFileSystem, RealFileSystem>()
            .AddSingleton<IScrapeOrchestrator, ScrapeOrchestrator>()
            .AddSingleton<MainWindowViewModel>()
            .AddSingleton<MainWindow>()
            .AddDbContextFactory<ControlDbContext>((serviceProvider, options) =>
                options.UseSqlite(serviceProvider.GetRequiredService<IOptions<ScrapeConfiguration>>().Value.ConnectionStrings.Sqlite))
            .AddVelopackUpdates(configuration)
            .AddVelopackUpdateNotifications();
}
