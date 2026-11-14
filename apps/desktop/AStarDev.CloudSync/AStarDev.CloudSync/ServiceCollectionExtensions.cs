using System.Globalization;
using System.IO.Abstractions;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.CloudSync.Accounts.Sqlite;
using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.Sync.OneDrive;
using AStarDev.CloudSync.Sync.Sqlite;
using AStarDev.CloudSync.Theming;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.SyncRestart;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.Localization;
using AStarDev.SingleInstance;
using AStarDev.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;

namespace AStarDev.CloudSync;

/// <summary>Registers the application's services with the dependency injection container.</summary>
public static class ServiceCollectionExtensions
{
    private const string ApplicationName = "AStarDev.CloudSync";

    private static string SettingsFilePath => ApplicationName.ApplicationDirectory().CombinePath("display-preferences.json");

    private static string DatabaseFilePath => ApplicationName.ApplicationDirectory().CombinePath("cloudsync.db");

    private static string TokenCacheFolderPath => ApplicationName.ApplicationDirectory();

    private const string SingleInstanceName = "cloudsync";

    private static string TokenCacheConsentFilePath => ApplicationName.ApplicationDirectory().CombinePath("token-cache-consent.json");

    /// <summary>Registers localisation, the Velopack update services, the account-store overview, the placeholder sync actions, the add-account dialog and its token cache question, the sync restart question, the CloudSync database and account store, the Microsoft account sign-in and OneDrive drive lookup, the display settings with the plaintext token file setting, and the view models, including layouts A, B and C.</summary>
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
            .AddSyncOverview()
            .AddSingleton<ISyncActions, PlaceholderSyncActions>()
            .AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>()
            .AddSingleton<TokenCacheConsentViewModel>()
            .AddSingleton<ITokenCacheConsent>(provider => provider.GetRequiredService<TokenCacheConsentViewModel>())
            .AddSingleton<ConfirmSyncRestartViewModel>()
            .AddSingleton<IConfirmSyncRestartDialog>(provider => provider.GetRequiredService<ConfirmSyncRestartViewModel>())
            .AddSingleton<AddAccountViewModel>()
            .AddSingleton<AccountDialogViewModel>()
            .AddSingleton<FolderSelectionViewModel>()
            .AddSingleton<IFolderSelectionDialog>(provider => provider.GetRequiredService<FolderSelectionViewModel>())
            .AddSingleton<IAccountCommands>(provider => provider.GetRequiredService<AccountDialogViewModel>())
            .AddSingleton<SignInStateOverviewReloader>()
            .AddSingleton<SyncRunOverviewReloader>()
            .AddSingleton<IAddAccountDialog>(provider => provider.GetRequiredService<AddAccountViewModel>())
            .AddSingleton<IFileSystem, RealFileSystem>()
            .AddSingleInstance()
            .AddSingleton(provider => SingleInstanceOptionsFactory.Create(provider.GetRequiredService<IFileSystem>().DirectoryInfo.New(ApplicationName.ApplicationDirectory()), SingleInstanceName, null))
            .AddCloudSyncPersistence(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(DatabaseFilePath))
            .AddOneDriveAccounts(configuration, provider => provider.GetRequiredService<IFileSystem>().DirectoryInfo.New(TokenCacheFolderPath), provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(TokenCacheConsentFilePath))
            .AddCloudSync()
            .AddOneDriveSync()
            .AddSyncPersistence(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(DatabaseFilePath))
            .AddDisplayPreferences(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(SettingsFilePath))
            .AddSingleton<IThemeApplier, AvaloniaThemeApplier>()
            .AddSingleton<IHomeFolder, UserProfileHomeFolder>()
            .AddSingleton<IFolderPicker, AvaloniaFolderPicker>()
            .AddSingleton<LocalPathFormatter>()
            .AddSingleton<PlaintextCacheConsentSettingsViewModel>()
            .AddSingleton<DisplaySettingsViewModel>()
            .AddSingleton<SyncOverviewViewModel>()
            .AddSingleton<LayoutBViewModel>()
            .AddSingleton<LayoutAViewModel>()
            .AddSingleton<LayoutCViewModel>()
            .AddSingleton<MainWindowViewModel>();
}
