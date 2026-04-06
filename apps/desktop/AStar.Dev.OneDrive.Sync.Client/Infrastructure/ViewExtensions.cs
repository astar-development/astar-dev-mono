using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using AStar.Dev.OneDrive.Sync.Client.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure;

/// <summary>
/// When refactored, this class will disappear but "baby steps"
/// </summary>
public static class ViewExtensions
{
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        _ = services.AddSingleton<MainWindow>();
        _ = services.AddSingleton<AccountsView>();
        _ = services.AddSingleton<ActivityView>();
        _ = services.AddSingleton<AddAccountWizardView>();
        _ = services.AddSingleton<DashboardView>();
        _ = services.AddSingleton<FilesView>();
        _ = services.AddSingleton<FolderTreeItemView>();
        _ = services.AddSingleton<SettingsView>();

        return services;
    }
}
