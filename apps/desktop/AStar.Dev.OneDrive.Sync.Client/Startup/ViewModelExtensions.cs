using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;
using Microsoft.Extensions.DependencyInjection;
using AccountsViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountsViewModel;
using AccountSyncSettingsViewModel = AStar.Dev.OneDrive.Sync.Client.Accounts.AccountSyncSettingsViewModel;
using ActivityViewModel = AStar.Dev.OneDrive.Sync.Client.Activity.ActivityViewModel;
using DashboardViewModel = AStar.Dev.OneDrive.Sync.Client.Dashboard.DashboardViewModel;
using FilesViewModel = AStar.Dev.OneDrive.Sync.Client.Home.FilesViewModel;
using SettingsViewModel = AStar.Dev.OneDrive.Sync.Client.Settings.SettingsViewModel;
using StatusBarViewModel = AStar.Dev.OneDrive.Sync.Client.Home.StatusBarViewModel;

namespace AStar.Dev.OneDrive.Sync.Client.Startup;

internal static class ViewModelExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        _ = services.AddTransient<IApplicationInitializer, ApplicationInitializer>();

        _ = services.AddSingleton<MainWindowViewModel>();
        _ = services.AddSingleton<AccountsViewModel>();
        _ = services.AddSingleton<ActivityViewModel>();
        _ = services.AddSingleton<DashboardViewModel>();
        _ = services.AddSingleton<FilesViewModel>();
        _ = services.AddSingleton<FileClassificationRulesViewModel>();
        _ = services.AddSingleton<SettingsViewModel>();
        _ = services.AddSingleton<StatusBarViewModel>();

        _ = services.AddTransient<AccountSyncSettingsViewModel>();

        _ = services.AddSingleton<IAccountCardViewModelFactory, AccountCardViewModelFactory>();
        _ = services.AddSingleton<IAccountFilesViewModelFactory, AccountFilesViewModelFactory>();
        _ = services.AddSingleton<IActivityItemViewModelFactory, ActivityItemViewModelFactory>();
        _ = services.AddSingleton<IAddAccountWizardViewModelFactory, AddAccountWizardViewModelFactory>();
        _ = services.AddSingleton<IConflictItemViewModelFactory, ConflictItemViewModelFactory>();
        _ = services.AddSingleton<IDashboardAccountViewModelFactory, DashboardAccountViewModelFactory>();
        _ = services.AddSingleton<IFolderTreeNodeViewModelFactory, FolderTreeNodeViewModelFactory>();

        return services;
    }
}
