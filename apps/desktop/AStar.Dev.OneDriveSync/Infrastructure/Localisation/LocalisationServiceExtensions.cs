using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Localisation;

/// <summary>Registers all localisation services.</summary>
internal static class LocalisationServiceExtensions
{
    /// <summary>Adds <see cref="ILocalisationService" /> and <see cref="IRelativeTimeFormatter" /> to <paramref name="services" />.</summary>
    public static IServiceCollection AddLocalisation(this IServiceCollection services)
    {
        _ = services.AddSingleton<ILocalisationService, LocalisationService>();
        _ = services.AddSingleton<IRelativeTimeFormatter, RelativeTimeFormatter>();

        return services;
    }
}
