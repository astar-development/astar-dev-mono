using AStar.Dev.OneDrive.Sync.Client.Accounts;

namespace AStar.Dev.OneDrive.Sync.Client.Dashboard;

/// <summary>Creates <see cref="DashboardAccountViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IDashboardAccountViewModelFactory
{
    /// <summary>Creates a dashboard section view model for the supplied account.</summary>
    DashboardAccountViewModel Create(OneDriveAccount account);
}
