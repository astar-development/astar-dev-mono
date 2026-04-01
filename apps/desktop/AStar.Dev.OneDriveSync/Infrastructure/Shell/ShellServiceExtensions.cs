using AStar.Dev.OneDriveSync.Features.About;
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
    internal static IServiceCollection AddShell(this IServiceCollection services)
    {
        _ = services.AddLocalisation();
        _ = services.AddTheming();

        var featureAvailability = new FeatureAvailabilityService();
        RegisterAvailableFeatures(featureAvailability);
        featureAvailability.Freeze();

        _ = services.AddSingleton<IFeatureAvailabilityService>(featureAvailability);
        _ = services.AddSingleton<INavigationService, NavigationService>();

        _ = services.AddSingleton<IAccountRepository, AccountRepository>();
        _ = services.AddSingleton<IUserTypeService, UserTypeService>();

        _ = services.AddTransient<DashboardViewModel>();
        _ = services.AddTransient<AccountsViewModel>();
        _ = services.AddTransient<ActivityViewModel>();
        _ = services.AddTransient<ConflictsViewModel>();
        _ = services.AddTransient<LogViewerViewModel>();
        _ = services.AddTransient<SettingsViewModel>();
        _ = services.AddTransient(sp => OnboardingViewModel.CreateAsync(sp.GetRequiredService<IAccountRepository>()).GetAwaiter().GetResult());
        _ = services.AddTransient<HelpViewModel>();
        _ = services.AddTransient<AboutViewModel>();

        _ = services.AddTransient<MainWindowViewModel>();

        return services;
    }

    private static void RegisterAvailableFeatures(FeatureAvailabilityService service)
    {
        service.Register(NavSection.Dashboard);
        service.Register(NavSection.Settings);
        service.Register(NavSection.Help);
    }
}
