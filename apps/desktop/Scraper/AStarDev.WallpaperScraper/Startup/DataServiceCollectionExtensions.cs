using AStarDev.ControlDb;
using AStarDev.Utilities;
using AStarDev.WallpaperScraper.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.WallpaperScraper.Startup;

public static class DataServiceCollectionExtensions
{
    /// <summary>Registers the scrape configuration repository with the dependency injection container.</summary>
    /// <param name="services">The service collection to register the data services with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        _ = services.AddDbContextFactory<ControlDbContext>(ConfigureDbContext, ServiceLifetime.Singleton);
        services.AddScoped<IScrapeConfigurationRepository, ScrapeConfigurationRepository>();

        return services;
    }

    private static void ConfigureDbContext(DbContextOptionsBuilder builder)
    {
        string dbPath = ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory().CombinePath(Path.DirectorySeparatorChar.ToString()).CombinePath("data").CombinePath(Path.DirectorySeparatorChar.ToString()).CombinePath("astar-control.db");
        _ = builder.UseSqlite($"Data Source={dbPath}");
    }
}
