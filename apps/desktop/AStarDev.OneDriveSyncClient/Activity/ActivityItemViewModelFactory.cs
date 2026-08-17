using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Activity;

/// <summary>Container-backed factory for <see cref="ActivityItemViewModel"/> instances.</summary>
public sealed class ActivityItemViewModelFactory(ILocalizationService localizationService) : IActivityItemViewModelFactory
{
    /// <inheritdoc />
    public ActivityItemViewModel Create(string fileName) => new(localizationService) { FileName = fileName };

    /// <inheritdoc />
    public ActivityItemViewModel CreateInfo(string accountId, string fileName) => new(localizationService) { AccountId = accountId, FileName = fileName, Type = ActivityItemType.Info };

    /// <inheritdoc />
    public ActivityItemViewModel CreateFromJob(SyncJob job, string accountEmail) => ActivityItemViewModel.FromJob(job, localizationService, accountEmail);

    /// <inheritdoc />
    public ActivityItemViewModel CreateError(string accountId, string accountEmail, string message) => ActivityItemViewModel.Error(accountId, localizationService, accountEmail, message);
}
