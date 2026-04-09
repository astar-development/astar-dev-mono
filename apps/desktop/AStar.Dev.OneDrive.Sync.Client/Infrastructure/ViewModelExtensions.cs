using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Home;
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
using FilesViewModel = AStar.Dev.OneDrive.Sync.Client.Home.FilesViewModel;
using FolderTreeNodeViewModel = AStar.Dev.OneDrive.Sync.Client.Home.FolderTreeNodeViewModel;
using SettingsViewModel = AStar.Dev.OneDrive.Sync.Client.Settings.SettingsViewModel;
using StatusBarViewModel = AStar.Dev.OneDrive.Sync.Client.Home.StatusBarViewModel;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure;

/// <summary>
/// Add current view models to the DI container here. Transient for view models with short-lived state (e.g. wizards, dialogs), singleton for those that should maintain state across the app (e.g. accounts, activity).
/// </summary>
public static class ViewModelExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        _ = services.AddTransient<MainWindowViewModel>();
        _ = services.AddTransient<AccountsViewModel>();
        _ = services.AddTransient<ActivityViewModel>();
        _ = services.AddTransient<AccountCardViewModel>();
        _ = services.AddTransient<AccountFilesViewModel>();
        _ = services.AddTransient<AccountSyncSettingsViewModel>();
        _ = services.AddTransient<ActivityItemViewModel>();
        _ = services.AddTransient<ActivityViewModel>();
        _ = services.AddTransient<AddAccountWizardViewModel>();
        _ = services.AddTransient<Func<AddAccountWizardViewModel>>(provider => provider.GetRequiredService<AddAccountWizardViewModel>);
        _ = services.AddTransient<ConflictItemViewModel>();
        _ = services.AddTransient<DashboardAccountViewModel>();
        _ = services.AddTransient<DashboardViewModel>();
        _ = services.AddTransient<FilesViewModel>();
        _ = services.AddTransient<FolderTreeNodeViewModel>();
        _ = services.AddTransient<StatusBarViewModel>();
        _ = services.AddTransient<SettingsViewModel>();

        return services;
    }
}
