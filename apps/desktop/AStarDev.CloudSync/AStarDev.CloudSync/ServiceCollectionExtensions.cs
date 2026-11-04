using System.Globalization;
using System.IO.Abstractions;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Core;
using AStarDev.CloudSync.Core.Placeholder;
using AStarDev.CloudSync.Settings;
using AStarDev.CloudSync.Theming;
using AStarDev.CloudSync.ViewModels;
using AStarDev.Localization;
using AStarDev.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions;

namespace AStarDev.CloudSync;

/// <summary>Registers the application's services with the dependency injection container.</summary>
public static class ServiceCollectionExtensions
{
    private static string SettingsFilePath => "AStarDev.CloudSync".ApplicationDirectory().CombinePath("display-preferences.json");

    /// <summary>Registers localisation, the Velopack update services, the placeholder sync data, the display settings and the view models, including layouts A, B and C.</summary>
    /// <param name="services">The service collection to register the application's services with.</param>
    /// <param name="configuration">The application configuration used to bind the options sections.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddLocalizationService(typeof(ServiceCollectionExtensions).Assembly, new CultureInfo("en-GB"))
            .AddVelopackUpdates(configuration)
            .AddVelopackUpdateNotifications()
            .AddSingleton<IUpdateDialogTextProvider, LocalizedUpdateDialogTextProvider>()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ISyncOverviewSource, PlaceholderSyncOverviewSource>()
            .AddSingleton<IFileSystem, RealFileSystem>()
            .AddSingleton<IDisplayPreferencesStore>(provider => new JsonDisplayPreferencesStore(provider.GetRequiredService<IFileSystem>().FileInfo.New(SettingsFilePath), provider.GetRequiredService<ILogger<JsonDisplayPreferencesStore>>()))
            .AddSingleton<IThemeApplier, AvaloniaThemeApplier>()
            .AddSingleton<DisplaySettingsViewModel>()
            .AddSingleton<SyncOverviewViewModel>()
            .AddSingleton<LayoutBViewModel>()
            .AddSingleton<LayoutAViewModel>()
            .AddSingleton<LayoutCViewModel>()
            .AddSingleton<MainWindowViewModel>();
}
