using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using AccountCardViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountCardViewModel;
using AccountFilesViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountFilesViewModel;
using AccountsViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountsViewModel;
using AccountSyncSettingsViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountSyncSettingsViewModel;
using ActivityViewModel = AStar.Dev.OneDrive.Sync.Client.Activity.ActivityViewModel;
using AddAccountWizardViewModel = AStar.Dev.OneDrive.Sync.Client.Onboarding.AddAccountWizardViewModel;
using ConflictItemViewModel = AStar.Dev.OneDrive.Sync.Client.Conflicts.ConflictItemViewModel;
using DashboardAccountViewModel = AStar.Dev.OneDrive.Sync.Client.Dashboard.DashboardAccountViewModel;
using DashboardViewModel = AStar.Dev.OneDrive.Sync.Client.Dashboard.DashboardViewModel;
using SettingsViewModel = AStar.Dev.OneDrive.Sync.Client.Settings.SettingsViewModel;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure;

/// <summary>
/// When refactored, this class will disappear but "baby steps"
/// </summary>
public static class ViewModelExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        _ = services.AddTransient<MainWindowViewModel>();
        _ = services.AddSingleton<AccountsViewModel>();
        _ = services.AddSingleton<ActivityViewModel>();
        _ = services.AddSingleton<AccountCardViewModel>();
        _ = services.AddSingleton<AccountFilesViewModel>();
        _ = services.AddSingleton<AccountSyncSettingsViewModel>();
        _ = services.AddSingleton<ActivityItemViewModel>();
        _ = services.AddSingleton<ActivityViewModel>();
        _ = services.AddTransient<AddAccountWizardViewModel>();
        _ = services.AddSingleton<Func<AddAccountWizardViewModel>>(provider => provider.GetRequiredService<AddAccountWizardViewModel>);
        _ = services.AddSingleton<ConflictItemViewModel>();
        _ = services.AddSingleton<DashboardAccountViewModel>();
        _ = services.AddSingleton<DashboardViewModel>();
        _ = services.AddSingleton<FilesViewModel>();
        _ = services.AddSingleton<FolderTreeNodeViewModel>();
        _ = services.AddTransient<SettingsViewModel>();
        _ = services.AddSingleton<StatusBarViewModel>();
        _ = services.AddTransient<SettingsViewModel>();

        return services;
    }
}
