using System.Reflection;
using AStarDev.CloudSync.ViewModels.Logging;
using System.Runtime.InteropServices;
using System.Globalization;
using System.IO.Abstractions;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Accounts.OneDrive;
using AStarDev.CloudSync.Accounts.Sqlite;
using AStarDev.CloudSync.Autostart;
using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.Classification.Sqlite;
using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.Logging;
using AStarDev.CloudSync.Notifications;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.Sync.OneDrive;
using AStarDev.CloudSync.Sync.Sqlite;
using AStarDev.CloudSync.Theming;
using AStarDev.DependencyInjection;
using AStarDev.CloudSync.Updates;
using AStarDev.CloudSync.Tray;
using AStarDev.CloudSync.ViewModels;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.CloudSync.ViewModels.Classification;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.CloudSync.ViewModels.Notifications;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.SyncRestart;
using AStarDev.CloudSync.ViewModels.Tray;
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

    private static string TrayNoticeFilePath => ApplicationName.ApplicationDirectory().CombinePath("tray-notice-shown");

    private static string UpdateDeferralFilePath => ApplicationName.ApplicationDirectory().CombinePath("update-deferral.json");

    private static string DatabaseFilePath => ApplicationName.ApplicationDirectory().CombinePath("cloudsync.db");

    private static string TokenCacheFolderPath => ApplicationName.ApplicationDirectory();

    private const string SingleInstanceName = "cloudsync";

    private static string TokenCacheConsentFilePath => ApplicationName.ApplicationDirectory().CombinePath("token-cache-consent.json");

    /// <summary>Registers localisation, the Velopack update services, the account-store overview, the placeholder sync actions, the add-account dialog and its token cache question, the sync restart question, the classification module and its page, the CloudSync database and account store, the Microsoft account sign-in and OneDrive drive lookup, the display settings with the plaintext token file setting, and the view models, including layouts A, B and C.</summary>
    /// <param name="services">The service collection to register the application's services with.</param>
    /// <param name="configuration">The application configuration used to bind the options sections.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddLocalizationService(typeof(ServiceCollectionExtensions).Assembly, new CultureInfo("en-GB"))
            .AddVelopackUpdates(configuration)
            .AddVelopackUpdateNotifications()
            .AddVelopackUpdateScheduling()
            .AddVelopackUpdateDeferral(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(UpdateDeferralFilePath))
            .AddSingleton<IUpdateApplyGate, CloudSyncUpdateApplyGate>()
            .AddSingleton<IUpdateDialogTextProvider, LocalizedUpdateDialogTextProvider>()
            .AddSingleton(TimeProvider.System)
            .AddSyncOverview()
            .AddSingleton<ISyncActions, PlaceholderSyncActions>()
            .AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>()
            .AddSingleton<MainWindowVisibility>()
            .AddSingleton<IAutostartLauncher, VelopackAutostartLauncher>()
            .AddSingleton<IAutostartRegistration>(provider => AutostartRegistrationFor(provider))
            .AddSingleton<AutostartSettingsViewModel>()
            .AddSingleton<IWindowPresence, WindowPresenceReader>()
            .AddSingleton<INotificationSender>(_ => NotificationSenderFactory.Create(RuntimeInformation.IsOSPlatform))
            .AddSingleton<PassNotificationDecider>()
            .AddSingleton<SyncNotifier>()
            .AddSingleton<IMainWindowVisibility>(provider => provider.GetRequiredService<MainWindowVisibility>())
            .AddSingleton<IApplicationExit, DesktopApplicationExit>()
            .AddSingleton<ITrayNoticeStore>(provider => ActivatorUtilities.CreateInstance<FileTrayNoticeStore>(provider, provider.GetRequiredService<IFileSystem>().FileInfo.New(TrayNoticeFilePath)))
            .AddSingleton<TrayCloseNotice>()
            .AddSingleton<ITrayAvailability, DesktopTrayAvailability>()
            .AddSingleton<WindowClosePolicy>()
            .AddSingleton<ConfirmQuitViewModel>()
            .AddSingleton<IConfirmQuitDialog>(provider => provider.GetRequiredService<ConfirmQuitViewModel>())
            .AddSingleton<IAppShutdown, AppShutdown>()
            .AddSingleton<TrayMenuModel>()
            .AddSingleton<TrayIconService>()
            .AddSingleton<TokenCacheConsentViewModel>()
            .AddSingleton<ITokenCacheConsent>(provider => provider.GetRequiredService<TokenCacheConsentViewModel>())
            .AddSingleton<ConfirmSyncRestartViewModel>()
            .AddSingleton<IConfirmSyncRestartDialog>(provider => provider.GetRequiredService<ConfirmSyncRestartViewModel>())
            .AddSingleton<AddAccountViewModel>()
            .AddSingleton<AccountDialogViewModel>()
            .AddSingleton<FolderSelectionViewModel>()
            .AddSingleton<IFolderSelectionDialog>(provider => provider.GetRequiredService<FolderSelectionViewModel>())
            .AddSingleton<IFolderSelectionFactory, FolderSelectionFactory>()
            .AddSingleton<IActivityListFactory, ActivityListFactory>()
            .AddSingleton<IAccountCommands>(provider => provider.GetRequiredService<AccountDialogViewModel>())
            .AddSingleton<SignInStateOverviewReloader>()
            .AddSingleton<SyncRunOverviewReloader>()
            .AddSingleton<IAddAccountDialog>(provider => provider.GetRequiredService<AddAccountViewModel>())
            .AddSingleton<IFileSystem, RealFileSystem>()
            .AddSingleton<LogFileSweeper>()
            .AddSingleton<ILogFileLocator, CloudSyncLogFileLocator>()
            .AddSingleton<ILogEntryReader, LogEntryReader>()
            .AddSingleton<ILogDiagnosticsClipboard, AvaloniaLogDiagnosticsClipboard>()
            .AddSingleton<LogViewerViewModel>()
            .AddSingleton<LogViewerLauncher>()
            .AddSingleton<UnhandledExceptionLogger>()
            .AddSingleInstance()
            .AddSingleton(provider => SingleInstanceOptionsFactory.Create(provider.GetRequiredService<IFileSystem>().DirectoryInfo.New(ApplicationName.ApplicationDirectory()), SingleInstanceName, null))
            .AddCloudSyncPersistence(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(DatabaseFilePath))
            .AddOneDriveAccounts(configuration, provider => provider.GetRequiredService<IFileSystem>().DirectoryInfo.New(TokenCacheFolderPath), provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(TokenCacheConsentFilePath))
            .AddCloudSync()
            .AddOneDriveSync()
            .AddSyncPersistence(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(DatabaseFilePath))
            .AddClassificationPersistence(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(DatabaseFilePath))
            .AddClassification()
            .AddSerialMigrations()
            .AddSingleton<SyncStartup>()
            .AddDisplayPreferences(provider => provider.GetRequiredService<IFileSystem>().FileInfo.New(SettingsFilePath))
            .AddSingleton<IThemeApplier, AvaloniaThemeApplier>()
            .AddSingleton<IHomeFolder, UserProfileHomeFolder>()
            .AddSingleton<IFolderPicker, AvaloniaFolderPicker>()
            .AddSingleton<LocalPathFormatter>()
            .AddSingleton<PlaintextCacheConsentSettingsViewModel>()
            .AddSingleton<SyncScheduleSettingsViewModel>()
            .AddSingleton<TrashRetentionSettingsViewModel>()
            .AddSingleton<AccountLogLevelsViewModel>()
            .AddSingleton<FileAuditViewModel>()
            .AddSingleton(provider => DisplaySettingsSectionsFactory.Create(provider.GetRequiredService<SyncScheduleSettingsViewModel>(), provider.GetRequiredService<TrashRetentionSettingsViewModel>(), provider.GetRequiredService<AutostartSettingsViewModel>(), provider.GetRequiredService<AccountLogLevelsViewModel>(), provider.GetRequiredService<FileAuditViewModel>()))
            .AddSingleton<DisplaySettingsViewModel>()
            .AddSingleton<SyncOverviewViewModel>()
            .AddSingleton<ConflictListSources>()
            .AddSingleton<ConflictListViewModel>()
            .AddSingleton<ClassificationTermListViewModel>()
            .AddSingleton<ClassificationSuggestionsViewModel>()
            .AddSingleton<ClassificationTabsViewModel>()
            .AddSingleton<ReclassifyViewModel>()
            .AddSingleton<ClassificationPageViewModel>()
            .AddSingleton<IConflictListFactory, ConflictListFactory>()
            .AddSingleton<LayoutBViewModel>()
            .AddSingleton<LayoutAViewModel>()
            .AddSingleton<LayoutCViewModel>()
            .AddSingleton<MainWindowLayouts>()
            .AddSingleton<DatabaseHealthBannerViewModel>()
            .AddSingleton<MainWindowDialogs>()
            .AddSingleton(_ => AppVersionFactory.Create(typeof(ServiceCollectionExtensions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion))
            .AddSingleton<MainWindowViewModel>();

    private static IServiceCollection AddSerialMigrations(this IServiceCollection services)
    {
        var migrations = services.Last(descriptor => descriptor.ServiceType == typeof(ICloudSyncDatabase));

        return services.AddSingleton<ICloudSyncDatabase>(provider => new SerialCloudSyncDatabase(migrations.CreateInstance<ICloudSyncDatabase>(provider)));
    }

    private static IAutostartRegistration AutostartRegistrationFor(IServiceProvider provider)
    {
        if (OperatingSystem.IsLinux())
            return ActivatorUtilities.CreateInstance<LinuxAutostartRegistration>(provider);

        if (OperatingSystem.IsWindows())
            return ActivatorUtilities.CreateInstance<WindowsAutostartRegistration>(provider, new RegistryRunKeyStore());

        if (OperatingSystem.IsMacOS())
            return ActivatorUtilities.CreateInstance<MacAutostartRegistration>(provider);

        return new UnsupportedAutostartRegistration();
    }
}
