using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Velopack.Publishing;

/// <summary>Registers the Velopack update-checking service and its configuration binding with the dependency injection container.</summary>
public static class VelopackUpdateServiceCollectionExtensions
{
    /// <summary>Binds <see cref="VelopackUpdateSettings"/> from <paramref name="configuration"/> and registers <see cref="IVelopackUpdateService"/>.</summary>
    /// <param name="services">The service collection to register the update services with.</param>
    /// <param name="configuration">The application configuration used to bind <see cref="VelopackUpdateSettings"/>.</param>
    /// <returns>The <paramref name="services"/> collection to allow further chaining.</returns>
    public static IServiceCollection AddVelopackUpdates(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.AddOptions<VelopackUpdateSettings>()
            .Bind(configuration.GetSection(VelopackUpdateSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        _ = services.AddSingleton<IVelopackUpdateService, VelopackUpdateService>();

        return services;
    }
}
