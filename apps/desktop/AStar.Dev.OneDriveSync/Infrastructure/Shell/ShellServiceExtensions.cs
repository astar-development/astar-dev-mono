using System;
using AStar.Dev.Conflict.Resolution;
using AStar.Dev.Conflict.Resolution.Features.Persistence;
using AStar.Dev.OneDrive.Client;
using AStar.Dev.OneDriveSync.Features.About;
using System.IO.Abstractions;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.Activity;
using AStar.Dev.OneDriveSync.Features.Conflicts;
using AStar.Dev.Sync.Engine.Features.Activity;
using AStar.Dev.OneDriveSync.Features.Dashboard;
using AStar.Dev.OneDriveSync.Features.Help;
using AStar.Dev.OneDriveSync.Features.Home;
using AStar.Dev.OneDriveSync.Features.LogViewer;
using AStar.Dev.OneDriveSync.Features.Onboarding;
using AStar.Dev.OneDriveSync.Features.Settings;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.OneDriveSync.Infrastructure.Theming;
using AStar.Dev.Sync.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

internal static class ShellServiceExtensions
{
    internal static IServiceCollection AddShell(this IServiceCollection services, OneDriveClientOptions oneDriveOptions, InMemoryLogSink inMemoryLogSink)
    {
        _ = services.AddLocalisation();
        _ = services.AddTheming();
        _ = services.AddOneDriveClient(oneDriveOptions);
        _ = services.AddSyncEngine();

        var featureAvailability = new FeatureAvailabilityService();
        RegisterAvailableFeatures(featureAvailability);
        featureAvailability.Freeze();

        _ = services.AddSingleton<IFeatureAvailabilityService>(featureAvailability);
        _ = services.AddSingleton<INavigationService, NavigationService>();
        _ = services.AddSingleton<ShellNavigator>();
        _ = services.AddSingleton<IShellNavigator>(sp => sp.GetRequiredService<ShellNavigator>());

        _ = services.AddSingleton<ActivityFeedService>();
        _ = services.AddSingleton<IActivityFeedService>(sp => sp.GetRequiredService<ActivityFeedService>());
        _ = services.AddSingleton<IActivityReporter>(sp => sp.GetRequiredService<ActivityFeedService>());

        _ = services.AddConflictResolution();
        _ = services.AddSingleton<IConflictStore, ConflictStore>();

        _ = services.AddSingleton<InMemoryLogSink>(inMemoryLogSink);
        _ = services.AddSingleton<ILogEntryProvider>(inMemoryLogSink);

        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddSingleton<IAccountRepository, AccountRepository>();
        _ = services.AddSingleton<ILocalSyncPathService, LocalSyncPathService>();
        _ = services.AddSingleton<IUserTypeService, UserTypeService>();
        _ = services.AddSingleton<INotificationsService, NotificationsService>();

        _ = services.AddTransient<AddAccountWizardViewModel>();
        _ = services.AddSingleton<Func<AddAccountWizardViewModel>>(provider => provider.GetRequiredService<AddAccountWizardViewModel>);

        _ = services.AddSingleton<IDialogService, AvaloniaDialogService>();
        _ = services.AddSingleton<AvaloniaToastService>();
        _ = services.AddSingleton<IToastService>(sp => sp.GetRequiredService<AvaloniaToastService>());
        _ = services.AddSingleton<DashboardViewModel>();
        _ = services.AddSingleton<AccountsViewModel>();
        _ = services.AddTransient<ActivityViewModel>();
        _ = services.AddSingleton<ConflictsViewModel>();
        _ = services.AddTransient<LogViewerViewModel>();
        _ = services.AddTransient<SettingsViewModel>();
        _ = services.AddSingleton<OnboardingViewModel>();
        _ = services.AddSingleton<HelpViewModel>();
        _ = services.AddSingleton<AboutViewModel>();

        _ = services.AddTransient<MainWindowViewModel>();

        return services;
    }

    private static void RegisterAvailableFeatures(FeatureAvailabilityService service)
    {
        service.Register(NavSection.Dashboard);
        service.Register(NavSection.Accounts);
        service.Register(NavSection.Activity);
        service.Register(NavSection.Conflicts);
        service.Register(NavSection.LogViewer);
        service.Register(NavSection.Settings);
        service.Register(NavSection.Help);
    }
}
