using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Wallpaper.Scraper;

/// <summary>Registers the application's services with the dependency injection container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers configuration, infrastructure, scraping, and UI services with the dependency injection container.</summary>
    /// <param name="services">The service collection to register the application's services with.</param>
    /// <param name="configuration">The application configuration used to bind the options sections.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddConfigurationServices(configuration)
            .AddInfrastructureServices()
            .AddScrapingServices()
            .AddUiServices();
}
