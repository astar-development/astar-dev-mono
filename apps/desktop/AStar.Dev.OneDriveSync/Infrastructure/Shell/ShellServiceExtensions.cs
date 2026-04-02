using AStar.Dev.OneDrive.Client;
using AStar.Dev.OneDriveSync.Features.About;
using System.IO.Abstractions;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.Activity;
using AStar.Dev.OneDriveSync.Features.Conflicts;
using AStar.Dev.OneDriveSync.Features.Dashboard;
using AStar.Dev.OneDriveSync.Features.Help;
using AStar.Dev.OneDriveSync.Features.Home;
using AStar.Dev.OneDriveSync.Features.LogViewer;
using AStar.Dev.OneDriveSync.Features.Onboarding;
using AStar.Dev.OneDriveSync.Features.Settings;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

internal static class ShellServiceExtensions
{
    internal static IServiceCollection AddShell(this IServiceCollection services, OneDriveClientOptions oneDriveOptions)
    {
        _ = services.AddLocalisation();
        _ = services.AddTheming();
        _ = services.AddOneDriveClient(oneDriveOptions);

        var featureAvailability = new FeatureAvailabilityService();
        RegisterAvailableFeatures(featureAvailability);
        featureAvailability.Freeze();

        _ = services.AddSingleton<IFeatureAvailabilityService>(featureAvailability);
        _ = services.AddSingleton<INavigationService, NavigationService>();
        _ = services.AddSingleton<ShellNavigator>();
        _ = services.AddSingleton<IShellNavigator>(sp => sp.GetRequiredService<ShellNavigator>());

        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddSingleton<IAccountRepository, AccountRepository>();
        _ = services.AddSingleton<ILocalSyncPathService, LocalSyncPathService>();
        _ = services.AddSingleton<IUserTypeService, UserTypeService>();

        _ = services.AddTransient<AddAccountWizardViewModel>();
        _ = services.AddSingleton<Func<AddAccountWizardViewModel>>(provider => provider.GetRequiredService<AddAccountWizardViewModel>);

        _ = services.AddSingleton<DashboardViewModel>();
        _ = services.AddSingleton<AccountsViewModel>();
        _ = services.AddSingleton<ActivityViewModel>();
        _ = services.AddSingleton<ConflictsViewModel>();
        _ = services.AddSingleton<LogViewerViewModel>();
        _ = services.AddSingleton<SettingsViewModel>();
        _ = services.AddSingleton<OnboardingViewModel>();
        _ = services.AddSingleton<HelpViewModel>();
        _ = services.AddSingleton<AboutViewModel>();

        _ = services.AddSingleton<MainWindowViewModel>();

        return services;
    }

    private static void RegisterAvailableFeatures(FeatureAvailabilityService service)
    {
        service.Register(NavSection.Dashboard);
        service.Register(NavSection.Accounts);
        service.Register(NavSection.Settings);
        service.Register(NavSection.Help);
    }
}
