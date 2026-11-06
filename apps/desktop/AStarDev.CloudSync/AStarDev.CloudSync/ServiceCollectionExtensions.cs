using System.Globalization;
using System.IO.Abstractions;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Core.Common;
using AStarDev.CloudSync.Core.DisplaySettings;
using AStarDev.CloudSync.Core.Overview;
using AStarDev.CloudSync.Infrastructure.Common;
using AStarDev.CloudSync.Infrastructure.DisplaySettings;
using AStarDev.CloudSync.Infrastructure.OneDrive;
using AStarDev.CloudSync.Infrastructure.Overview;
using AStarDev.CloudSync.Infrastructure.Persistence;
using AStarDev.CloudSync.Theming;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Accounts;
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
    private const string ApplicationName = "AStarDev.CloudSync";

    private static string SettingsFilePath => ApplicationName.ApplicationDirectory().CombinePath("display-preferences.json");

    private static string DatabaseFilePath => ApplicationName.ApplicationDirectory().CombinePath("cloudsync.db");

    private static string TokenCacheFolderPath => ApplicationName.ApplicationDirectory();

    private static string TokenCacheConsentFilePath => ApplicationName.ApplicationDirectory().CombinePath("token-cache-consent.json");

    /// <summary>Registers localisation, the Velopack update services, the account-store overview, the placeholder sync actions, the add-account dialog and its token cache question, the CloudSync database and account store, the Microsoft account sign-in and OneDrive drive lookup, the display settings with the plaintext token file setting, and the view models, including layouts A, B and C.</summary>
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
            .AddSingleton<ISyncOverviewSource, AccountStoreSyncOverviewSource>()
            .AddSingleton<ISyncActions, PlaceholderSyncActions>()
            .AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>()
            .AddSingleton<TokenCacheConsentViewModel>()
            .AddSingleton<ITokenCacheConsent>(provider => provider.GetRequiredService<TokenCacheConsentViewModel>())
            .AddSingleton<AddAccountViewModel>()
            .AddSingleton<IAddAccountDialog>(provider => provider.GetRequiredService<AddAccountViewModel>())
            .AddSingleton<IFileSystem, RealFileSystem>()
            .AddCloudSyncPersistence(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(DatabaseFilePath))
            .AddOneDriveAccounts(configuration, provider => provider.GetRequiredService<IFileSystem>().DirectoryInfo.New(TokenCacheFolderPath), provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(TokenCacheConsentFilePath))
            .AddSingleton<IDisplayPreferencesStore>(provider => new JsonDisplayPreferencesStore(provider.GetRequiredService<IFileSystem>().FileInfo.New(SettingsFilePath), provider.GetRequiredService<ILogger<JsonDisplayPreferencesStore>>()))
            .AddSingleton<IThemeApplier, AvaloniaThemeApplier>()
            .AddSingleton<IHomeFolder, UserProfileHomeFolder>()
            .AddSingleton<LocalPathFormatter>()
            .AddSingleton<PlaintextCacheConsentSettingsViewModel>()
            .AddSingleton<DisplaySettingsViewModel>()
            .AddSingleton<SyncOverviewViewModel>()
            .AddSingleton<LayoutBViewModel>()
            .AddSingleton<LayoutAViewModel>()
            .AddSingleton<LayoutCViewModel>()
            .AddSingleton<MainWindowViewModel>();
}
