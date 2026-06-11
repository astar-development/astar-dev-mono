using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Dashboard;

/// <summary>Container-backed factory for <see cref="DashboardAccountViewModel"/> instances.</summary>
public sealed class DashboardAccountViewModelFactory(ISyncScheduler scheduler, IAccountRepository repository, ILocalizationService localizationService, IActivityItemViewModelFactory activityItemViewModelFactory) : IDashboardAccountViewModelFactory
{
    /// <inheritdoc />
    public DashboardAccountViewModel Create(OneDriveAccount account) => new(account, scheduler, repository, localizationService, activityItemViewModelFactory);
}
