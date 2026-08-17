using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Dashboard;

/// <summary>Container-backed factory for <see cref="DashboardAccountViewModel"/> instances.</summary>
public sealed class DashboardAccountViewModelFactory(ISyncScheduler scheduler, IAccountRepository repository, ILocalizationService localizationService, IActivityItemViewModelFactory activityItemViewModelFactory, ILogger<DashboardAccountViewModel> logger) : IDashboardAccountViewModelFactory
{
    /// <inheritdoc />
    public DashboardAccountViewModel Create(OneDriveAccount account) => new(account, scheduler, repository, localizationService, activityItemViewModelFactory, logger);
}
