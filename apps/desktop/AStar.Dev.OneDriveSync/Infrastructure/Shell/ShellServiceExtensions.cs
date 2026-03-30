using AStar.Dev.OneDriveSync.Features.About;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.Activity;
using AStar.Dev.OneDriveSync.Features.Conflicts;
using AStar.Dev.OneDriveSync.Features.Dashboard;
using AStar.Dev.OneDriveSync.Features.Help;
using AStar.Dev.OneDriveSync.Features.Home;
using AStar.Dev.OneDriveSync.Features.LogViewer;
using AStar.Dev.OneDriveSync.Features.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

internal static class ShellServiceExtensions
{
    internal static IServiceCollection AddShell(this IServiceCollection services)
    {
        var featureAvailability = new FeatureAvailabilityService();
        RegisterAvailableFeatures(featureAvailability);
        featureAvailability.Freeze();

        _ = services.AddSingleton<IFeatureAvailabilityService>(featureAvailability);
        _ = services.AddSingleton<INavigationService, NavigationService>();

        _ = services.AddSingleton<DashboardViewModel>();
        _ = services.AddSingleton<AccountsViewModel>();
        _ = services.AddSingleton<ActivityViewModel>();
        _ = services.AddSingleton<ConflictsViewModel>();
        _ = services.AddSingleton<LogViewerViewModel>();
        _ = services.AddSingleton<SettingsViewModel>();
        _ = services.AddSingleton<HelpViewModel>();
        _ = services.AddSingleton<AboutViewModel>();

        _ = services.AddSingleton<MainWindowViewModel>();

        return services;
    }

    private static void RegisterAvailableFeatures(FeatureAvailabilityService service)
        => service.Register(NavSection.Dashboard);
}
