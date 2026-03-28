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

        services.AddSingleton<IFeatureAvailabilityService>(featureAvailability);
        services.AddSingleton<INavigationService, NavigationService>();

        // Stub feature view models — singletons so each section retains state across navigations
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<AccountsViewModel>();
        services.AddSingleton<ActivityViewModel>();
        services.AddSingleton<ConflictsViewModel>();
        services.AddSingleton<LogViewerViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<HelpViewModel>();
        services.AddSingleton<AboutViewModel>();

        services.AddSingleton<MainWindowViewModel>();

        return services;
    }

    // Only Dashboard is a complete feature at this stage; all other sections are registered
    // by their owning feature story (NF-15: unimplemented features must appear disabled).
    private static void RegisterAvailableFeatures(FeatureAvailabilityService service)
        => service.Register(NavSection.Dashboard);
}
