using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Onboarding;
using AStarDev.OneDriveSyncClient.Infrastructure.OneDrive;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Infrastructure.Theme;
using AStarDev.OneDriveSyncClient.Infrastructure.Versioning;
using AStarDev.OneDriveSyncClient.Updates;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using AStarDev.LoggingSerilog.LogViewer;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;

namespace AStarDev.OneDriveSyncClient.Startup;

internal static class ShellServiceExtensions
{
    internal static IServiceCollection AddShell(this IServiceCollection services, InMemoryLogSink inMemoryLogSink)
    {
        var featureAvailability = new FeatureAvailabilityService();
        RegisterAvailableFeatures(featureAvailability);
        featureAvailability.Freeze();

        _ = services.AddSingleton<IFeatureAvailabilityService>(featureAvailability);
        _ = services.AddSingleton<IFeatureRegistrar>(featureAvailability);
        _ = services.AddSingleton<IFileManagerService, FileManagerService>();
        _ = services.AddTransient<IFileOpenerService, FileOpenerService>();
        _ = services.AddSingleton(inMemoryLogSink);
        _ = services.AddSingleton<ILogEntryProvider>(inMemoryLogSink);
        _ = services.AddSingleton<IFileSystem, RealFileSystem>();
        _ = services.AddSingleton(sp => new FileSystemServices(sp.GetRequiredService<IFileSystem>(), sp.GetRequiredService<IFileManagerService>()));
        _ = services.AddSingleton(TimeProvider.System);
        _ = services.AddSingleton<ITokenCacheService, TokenCacheService>();
        _ = services.AddSingleton<IAccountOnboardingService, AccountOnboardingService>();
        _ = services.AddSingleton<IAuthService, AuthService>();
        _ = services.AddSingleton<IGraphClientFactory, GraphClientFactory>();
        _ = services.AddSingleton<DriveContextCache>();
        _ = services.AddSingleton<GraphFolderEnumerator>();
        _ = services.AddSingleton<IGraphService, GraphService>();
        _ = services.AddSingleton<IQuotaRefreshService, QuotaRefreshService>();
        _ = services.AddSingleton<IStartupService, StartupService>();
        _ = services.AddSingleton<ISyncRuleService, SyncRuleService>();
        _ = services.AddSingleton<IHttpDownloader, HttpDownloader>();
        _ = services.AddSingleton<IUploadService, UploadService>();
        _ = services.AddSingleton<IConflictApplier, ConflictApplier>();
        _ = services.AddSingleton<ISyncedItemRegistrar, SyncedItemRegistrar>();
        _ = services.AddSingleton<IDownloadJobBuilder, DownloadJobBuilder>();
        _ = services.AddSingleton<ILocalChangeDetector, LocalChangeDetector>();
        _ = services.AddSingleton<IRemoteFolderEnumerator, RemoteFolderEnumerator>();
        _ = services.AddSingleton<IRemoteDeletionDetector, RemoteDeletionDetector>();
        _ = services.AddSingleton<ILocalDeletionDetector, LocalDeletionDetector>();
        _ = services.AddSingleton<ISyncJobExecutor, SyncJobExecutor>();
        _ = services.AddSingleton<SyncServiceDependencies>();
        _ = services.AddSingleton<ISyncPassOrchestrator, SyncPassOrchestrator>();
        _ = services.AddSingleton<ISyncService, SyncService>();
        _ = services.AddSingleton<ISyncScheduler, SyncScheduler>();
        _ = services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        _ = services.AddSingleton<IUiTimer, AvaloniaUiTimer>();
        _ = services.AddSingleton<ISyncEventAggregator, SyncEventAggregator>();
        _ = services.AddSingleton<ISettingsService, SettingsService>();
        _ = services.AddSingleton<IFolderPickerService, AvaloniaFolderPickerService>();
        _ = services.AddSingleton<IThemeService, ThemeService>();
        _ = services.AddSingleton<IJobHandler, DownloadJobHandler>();
        _ = services.AddSingleton<IJobHandler, UploadJobHandler>();
        _ = services.AddSingleton<IJobHandler, DeleteJobHandler>();
        _ = services.AddSingleton<ISyncWorkerFactory, SyncWorkerFactory>();
        _ = services.AddSingleton<ISyncPipeline, ParallelSyncPipeline>();
        _ = services.AddSingleton<IAppBootstrapper, AppBootstrapper>();
        _ = services.AddSingleton<IFileAutoCategorisor, RuleBasedFileAutoCategorisor>();
        _ = services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();
        _ = services.AddSingleton<IConfirmationDialogService, AvaloniaConfirmationDialogService>();
        _ = services.AddSingleton<ICategoryEditDialogService, AvaloniaCategoryEditDialogService>();
        _ = services.AddSingleton<IFileClassificationExportImportService, FileClassificationExportImportService>();
        _ = services.AddSingleton<IFileTypeClassifier, SyncClientFileTypeClassifier>();
        _ = services.AddSingleton<IApplicationVersionProvider, ApplicationVersionProvider>();
        _ = services.AddVelopackUpdateNotifications();
        _ = services.AddSingleton<IUpdateDialogTextProvider, LocalizedUpdateDialogTextProvider>();
        _ = services.AddOneDriveClient();

        return services;
    }

    private static void RegisterAvailableFeatures(FeatureAvailabilityService registrar)
    {
        _ = registrar.Register(NavSection.Dashboard);
        _ = registrar.Register(NavSection.Accounts);
        _ = registrar.Register(NavSection.Activity);
        _ = registrar.Register(NavSection.Conflicts);
        _ = registrar.Register(NavSection.LogViewer);
        _ = registrar.Register(NavSection.Classifications);
        _ = registrar.Register(NavSection.Settings);
        _ = registrar.Register(NavSection.Help);
    }
}
