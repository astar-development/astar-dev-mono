using System.Globalization;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.File.App.Updates;

/// <summary>Registers the Velopack update-check and update-notification services with the dependency injection container.</summary>
public static class VelopackServiceCollectionExtensions
{
    /// <summary>Registers localisation, <see cref="IVelopackUpdateService"/>, <see cref="IUpdateNotificationService"/>, and a localised <see cref="IUpdateDialogTextProvider"/>.</summary>
    /// <param name="services">The service collection to register the update services with.</param>
    /// <param name="configuration">The application configuration used to bind the <c>Updates</c> options section.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddVelopackUpdateServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddLocalizationService(typeof(VelopackServiceCollectionExtensions).Assembly, new CultureInfo("en-GB"))
            .AddVelopackUpdates(configuration)
            .AddVelopackUpdateNotifications()
            .AddSingleton<IUpdateDialogTextProvider, LocalizedUpdateDialogTextProvider>();
}
