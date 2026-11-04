using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.Localization;

/// <summary>Registers the localisation service with the dependency injection container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="ILocalizationService"/> that reads the localisation files embedded in <paramref name="resourceAssembly"/>, initialised to <paramref name="culture"/>.</summary>
    /// <param name="services">The service collection to register the localisation service with.</param>
    /// <param name="resourceAssembly">The assembly holding the <c>Assets/Localization/{culture}.json</c> embedded resources, usually the application assembly.</param>
    /// <param name="culture">The culture to load at startup.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddLocalizationService(this IServiceCollection services, Assembly resourceAssembly, CultureInfo culture)
    {
        var localization = new LocalizationService(resourceAssembly);
        localization.Initialise(culture);

        return services.AddSingleton<ILocalizationService>(localization);
    }
}
