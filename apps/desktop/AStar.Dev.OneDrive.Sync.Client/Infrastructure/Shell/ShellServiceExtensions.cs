using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.OneDrive;
using AStar.Dev.OneDrive.Sync.Client.LogViewer;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

internal static class ShellServiceExtensions
{
    internal static IServiceCollection AddShell(this IServiceCollection services, OneDriveClientOptions oneDriveOptions, InMemoryLogSink inMemoryLogSink)
    {
        var featureAvailability = new FeatureAvailabilityService();
        RegisterAvailableFeatures(featureAvailability);
        featureAvailability.Freeze();
        _ = services.AddOneDriveClient(oneDriveOptions);

        _ = services.AddSingleton<IFeatureAvailabilityService>(featureAvailability);

        _ = services.AddSingleton<InMemoryLogSink>(inMemoryLogSink);
        _ = services.AddSingleton<ILogEntryProvider>(inMemoryLogSink);
        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddSingleton<IAccountRepository, AccountRepository>();
        _ = services.AddTransient<IAuthService, AuthService>();
        _ = services.AddTransient<TokenCacheService>();
        _ = services.AddTransient<IGraphService, GraphService>();
        _ = services.AddTransient<IStartupService, StartupService>();
        _ = services.AddTransient<ISyncService,  SyncService>();
        _ = services.AddTransient<SyncScheduler>();
        _ = services.AddTransient<ISettingsService, SettingsService>();

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
