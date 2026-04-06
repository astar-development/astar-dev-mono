using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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
