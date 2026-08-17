using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Onboarding;
using AStarDev.OneDriveSyncClient.Search;
using AStarDev.OneDriveSyncClient.Settings;
using AStarDev.OneDriveSyncClient.Splash;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.Startup;

public static class ViewExtensions
{
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        _ = services.AddTransient<SplashWindow>();
        _ = services.AddSingleton<MainWindow>();
        _ = services.AddSingleton<AccountsView>();
        _ = services.AddSingleton<ActivityView>();
        _ = services.AddSingleton<AddAccountWizardView>();
        _ = services.AddSingleton<DashboardView>();
        _ = services.AddSingleton<FilesView>();
        _ = services.AddSingleton<FolderTreeItemView>();
        _ = services.AddSingleton<SettingsView>();
        _ = services.AddSingleton<SyncedFileSearchView>();

        return services;
    }
}
