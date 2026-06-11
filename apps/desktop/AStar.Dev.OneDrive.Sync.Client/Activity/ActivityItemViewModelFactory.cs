using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Activity;

/// <summary>Container-backed factory for <see cref="ActivityItemViewModel"/> instances.</summary>
public sealed class ActivityItemViewModelFactory(ILocalizationService localizationService) : IActivityItemViewModelFactory
{
    /// <inheritdoc />
    public ActivityItemViewModel Create(string fileName) => new(localizationService) { FileName = fileName };

    /// <inheritdoc />
    public ActivityItemViewModel CreateInfo(string accountId, string fileName) => new(localizationService) { AccountId = accountId, FileName = fileName, Type = ActivityItemType.Info };

    /// <inheritdoc />
    public ActivityItemViewModel CreateFromJob(SyncJob job, string accountEmail) => ActivityItemViewModel.FromJob(job, localizationService, accountEmail);
}
