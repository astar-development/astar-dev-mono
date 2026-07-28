using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Clock.Updates;

/// <summary>Registers the Velopack update-check and update-notification services with the dependency injection container.</summary>
public static class VelopackServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IVelopackUpdateService"/>, <see cref="IUpdateNotificationService"/>, and a plain <see cref="IUpdateDialogTextProvider"/>.</summary>
    /// <param name="services">The service collection to register the update services with.</param>
    /// <param name="configuration">The application configuration used to bind the <c>Updates</c> options section.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddVelopackUpdateServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddVelopackUpdates(configuration)
            .AddVelopackUpdateNotifications()
            .AddSingleton<IUpdateDialogTextProvider, PlainUpdateDialogTextProvider>();
}
