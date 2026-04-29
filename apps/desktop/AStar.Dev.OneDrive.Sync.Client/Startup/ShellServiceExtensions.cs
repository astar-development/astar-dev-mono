using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.OneDrive;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.LogViewer;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Startup;

internal static class ShellServiceExtensions
{
    internal static IServiceCollection AddShell(this IServiceCollection services, InMemoryLogSink inMemoryLogSink)
    {
        var featureAvailability = new FeatureAvailabilityService();
        RegisterAvailableFeatures(featureAvailability);
        featureAvailability.Freeze();

        _ = services.AddSingleton<IFeatureAvailabilityService>(featureAvailability);

        _ = services.AddSingleton(inMemoryLogSink);
        _ = services.AddSingleton<ILogEntryProvider>(inMemoryLogSink);
        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddSingleton<IAccountRepository, AccountRepository>();
        _ = services.AddSingleton<IDriveStateRepository, DriveStateRepository>();
        _ = services.AddSingleton<ISyncRuleRepository, SyncRuleRepository>();
        _ = services.AddSingleton<ISyncedItemRepository, SyncedItemRepository>();
        _ = services.AddSingleton<ITokenCacheService, TokenCacheService>();
        _ = services.AddSingleton<IAuthService, AuthService>();
        _ = services.AddSingleton<IGraphClientFactory, GraphClientFactory>();
        _ = services.AddSingleton<IGraphService, GraphService>();
        _ = services.AddTransient<IStartupService, StartupService>();
        _ = services.AddSingleton<IHttpDownloader, HttpDownloader>();
        _ = services.AddSingleton<IUploadService, UploadService>();
        _ = services.AddSingleton<ILocalChangeDetector, LocalChangeDetector>();
        _ = services.AddSingleton<IRemoteFolderEnumerator, RemoteFolderEnumerator>();
        _ = services.AddSingleton<IRemoteDeletionDetector, RemoteDeletionDetector>();
        _ = services.AddSingleton<ILocalDeletionDetector, LocalDeletionDetector>();
        _ = services.AddSingleton<ISyncJobExecutor, SyncJobExecutor>();
        _ = services.AddSingleton<SyncServiceDependencies>();
        _ = services.AddSingleton<ISyncService, SyncService>();
        _ = services.AddSingleton<ISyncScheduler, SyncScheduler>();
        _ = services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        _ = services.AddSingleton<ISyncEventAggregator, SyncEventAggregator>();
        _ = services.AddSingleton<ISettingsService, SettingsService>();
        _ = services.AddTransient<IThemeService, ThemeService>();
        _ = services.AddTransient<IParallelDownloadPipeline, ParallelDownloadPipeline>();
        _ = services.AddTransient<IAppBootstrapper, AppBootstrapper>();
        _ = services.AddOneDriveClient();

        return services;
    }

    private static void RegisterAvailableFeatures(FeatureAvailabilityService service)
    {
        service.Register(NavSection.Dashboard);
        service.Register(NavSection.Accounts);
        service.Register(NavSection.Activity);
        service.Register(NavSection.Conflicts);
        service.Register(NavSection.LogViewer);
        service.Register(NavSection.Settings);
        service.Register(NavSection.Help);
    }
}
