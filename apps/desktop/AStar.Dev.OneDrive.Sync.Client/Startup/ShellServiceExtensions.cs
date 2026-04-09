using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
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
    internal static IServiceCollection AddShell(this IServiceCollection services, OneDriveClientOptions oneDriveOptions, InMemoryLogSink inMemoryLogSink)
    {
        var featureAvailability = new FeatureAvailabilityService();
        RegisterAvailableFeatures(featureAvailability);
        featureAvailability.Freeze();
        _ = services.AddOneDriveClient(oneDriveOptions);

        _ = services.AddSingleton<IFeatureAvailabilityService>(featureAvailability);

        _ = services.AddSingleton(inMemoryLogSink);
        _ = services.AddSingleton<ILogEntryProvider>(inMemoryLogSink);
        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddSingleton<IAccountRepository, AccountRepository>();
        _ = services.AddTransient<IAuthService, AuthService>();
        _ = services.AddTransient<ITokenCacheService, TokenCacheService>();
        _ = services.AddTransient<IGraphService, GraphService>();
        _ = services.AddTransient<IStartupService, StartupService>();
        _ = services.AddTransient<ISyncService,  SyncService>();
        _ = services.AddTransient<ISyncScheduler, SyncScheduler>();
        _ = services.AddTransient<ISettingsService, SettingsService>();
        _ = services.AddTransient<ILocalChangeDetector, LocalChangeDetector>();
        _ = services.AddTransient<IUploadService, UploadService>();
        _ = services.AddTransient<IHttpDownloader, HttpDownloader>();
        _ = services.AddTransient<IThemeService, ThemeService>();

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
