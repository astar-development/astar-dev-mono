using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>Registers the display settings module.</summary>
public static class DisplaySettingsServiceCollectionExtensions
{
    /// <summary>Registers the <see cref="IDisplayPreferencesStore" /> that keeps the layout and theme in a JSON file.</summary>
    /// <param name="services">The service collection to register the store with.</param>
    /// <param name="settingsFile">Returns the JSON file the preferences are kept in.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddDisplayPreferences(this IServiceCollection services, Func<IServiceProvider, IFileInfo> settingsFile) => throw new NotImplementedException($"{services}{settingsFile}");
}
