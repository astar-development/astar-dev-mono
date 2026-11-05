using System.Globalization;
using System.IO.Abstractions;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Core.Common;
using AStarDev.CloudSync.Core.DisplaySettings;
using AStarDev.CloudSync.Core.Overview;
using AStarDev.CloudSync.Infrastructure.Common;
using AStarDev.CloudSync.Infrastructure.DisplaySettings;
using AStarDev.CloudSync.Infrastructure.OneDrive;
using AStarDev.CloudSync.Infrastructure.Persistence;
using AStarDev.CloudSync.Infrastructure.Placeholder;
using AStarDev.CloudSync.Theming;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.Overview;
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

    private static string DatabaseFilePath => "AStarDev.CloudSync".ApplicationDirectory().CombinePath("cloudsync.db");

    private static string TokenCacheFolderPath => "AStarDev.CloudSync".ApplicationDirectory();

    /// <summary>Registers localisation, the Velopack update services, the placeholder sync data and sync actions, the CloudSync database and account store, the Microsoft account sign-in and OneDrive drive lookup, the display settings and the view models, including layouts A, B and C.</summary>
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
            .AddSingleton<ISyncActions, PlaceholderSyncActions>()
            .AddSingleton<IFileSystem, RealFileSystem>()
            .AddCloudSyncPersistence(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(DatabaseFilePath))
            .AddOneDriveAccounts(configuration, provider => provider.GetRequiredService<IFileSystem>().DirectoryInfo.New(TokenCacheFolderPath))
            .AddSingleton<IDisplayPreferencesStore>(provider => new JsonDisplayPreferencesStore(provider.GetRequiredService<IFileSystem>().FileInfo.New(SettingsFilePath), provider.GetRequiredService<ILogger<JsonDisplayPreferencesStore>>()))
            .AddSingleton<IThemeApplier, AvaloniaThemeApplier>()
            .AddSingleton<IHomeFolder, UserProfileHomeFolder>()
            .AddSingleton<LocalPathFormatter>()
            .AddSingleton<DisplaySettingsViewModel>()
            .AddSingleton<SyncOverviewViewModel>()
            .AddSingleton<LayoutBViewModel>()
            .AddSingleton<LayoutAViewModel>()
            .AddSingleton<LayoutCViewModel>()
            .AddSingleton<MainWindowViewModel>();
}
