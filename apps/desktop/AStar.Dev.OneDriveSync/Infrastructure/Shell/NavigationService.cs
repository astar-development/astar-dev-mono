using System.Collections.Frozen;
using AStar.Dev.OneDriveSync.Features.About;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.Activity;
using AStar.Dev.OneDriveSync.Features.Conflicts;
using AStar.Dev.OneDriveSync.Features.Dashboard;
using AStar.Dev.OneDriveSync.Features.Help;
using AStar.Dev.OneDriveSync.Features.LogViewer;
using AStar.Dev.OneDriveSync.Features.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

// IServiceProvider is used here intentionally: NavigationService is infrastructure code
// that maps nav sections to view-model types. Using IServiceProvider avoids an
// 8-parameter constructor while still resolving singletons correctly at call time.
public sealed class NavigationService(IServiceProvider services) : INavigationService
{
    private static readonly FrozenDictionary<NavSection, Type> ViewModelTypes =
        new Dictionary<NavSection, Type>
        {
            [NavSection.Dashboard] = typeof(DashboardViewModel),
            [NavSection.Accounts]  = typeof(AccountsViewModel),
            [NavSection.Activity]  = typeof(ActivityViewModel),
            [NavSection.Conflicts] = typeof(ConflictsViewModel),
            [NavSection.LogViewer] = typeof(LogViewerViewModel),
            [NavSection.Settings]  = typeof(SettingsViewModel),
            [NavSection.Help]      = typeof(HelpViewModel),
            [NavSection.About]     = typeof(AboutViewModel),
        }.ToFrozenDictionary();

    public ViewModelBase ResolveView(NavSection section) =>
        (ViewModelBase)services.GetRequiredService(ViewModelTypes[section]);
}
