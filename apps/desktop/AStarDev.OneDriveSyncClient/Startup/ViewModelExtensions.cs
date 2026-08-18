using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Onboarding;
using AStarDev.OneDriveSyncClient.Search;
using Microsoft.Extensions.DependencyInjection;
using AccountsViewModel = AStarDev.OneDriveSyncClient.Accounts.AccountsViewModel;
using AccountSyncSettingsViewModel = AStarDev.OneDriveSyncClient.Accounts.AccountSyncSettingsViewModel;
using ActivityViewModel = AStarDev.OneDriveSyncClient.Activity.ActivityViewModel;
using DashboardViewModel = AStarDev.OneDriveSyncClient.Dashboard.DashboardViewModel;
using FilesViewModel = AStarDev.OneDriveSyncClient.Home.FilesViewModel;
using SettingsViewModel = AStarDev.OneDriveSyncClient.Settings.SettingsViewModel;
using StatusBarViewModel = AStarDev.OneDriveSyncClient.Home.StatusBarViewModel;

namespace AStarDev.OneDriveSyncClient.Startup;

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
        _ = services.AddSingleton<SyncedFileSearchViewModel>();

        _ = services.AddTransient<AccountSyncSettingsViewModel>();

        _ = services.AddSingleton<IAccountCardViewModelFactory, AccountCardViewModelFactory>();
        _ = services.AddSingleton<IAccountFilesViewServices>(sp => new AccountFilesViewServices(
            sp.GetRequiredService<IAuthService>(),
            sp.GetRequiredService<ILocalizationService>(),
            sp.GetRequiredService<IGraphService>(),
            sp.GetRequiredService<ISyncRuleService>()));
        _ = services.AddSingleton<IAccountFilesViewModelFactory, AccountFilesViewModelFactory>();
        _ = services.AddSingleton<IActivityItemViewModelFactory, ActivityItemViewModelFactory>();
        _ = services.AddSingleton<IAddAccountWizardViewModelFactory, AddAccountWizardViewModelFactory>();
        _ = services.AddSingleton<IConflictItemViewModelFactory, ConflictItemViewModelFactory>();
        _ = services.AddSingleton<IDashboardAccountViewModelFactory, DashboardAccountViewModelFactory>();
        _ = services.AddSingleton<IFolderTreeNodeViewModelFactory, FolderTreeNodeViewModelFactory>();

        return services;
    }
}
