using AStarDev.WallpaperScraper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.WallpaperScraper.Startup;

/// <summary>
/// Registers configuration bindings and configuration-derived services with the dependency injection container.
/// </summary>
public static class ConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the application configuration, its strongly-typed option sections, and the export directory.
    /// </summary>
    /// <param name="services">
    /// The service collection to register the configuration services with.
    /// </param>
    /// <param name="configuration">
    /// The application configuration used to bind the options sections.
    /// </param>
    /// <returns>
    /// The <paramref name="services" /> collection to allow further chaining.
    /// </returns>
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddOptions<ScrapeConfiguration>()
                .Bind(configuration.GetSection(ScrapeConfiguration.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        return services;
    }
}
