using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Activity;

/// <summary>Creates a new <see cref="ActivityListViewModel"/> over the shared services on every call.</summary>
/// <param name="reader">Reads the activity log.</param>
/// <param name="timeProvider">Supplies the current time for relative times.</param>
/// <param name="localization">Supplies the text and culture.</param>
/// <param name="logger">Reports a read that failed, without details.</param>
public sealed class ActivityListFactory(IActivityLogReader reader, TimeProvider timeProvider, ILocalizationService localization, ILogger<ActivityListViewModel> logger) : IActivityListFactory
{
    /// <inheritdoc />
    public ActivityListViewModel Create(SyncOverviewViewModel overview, Option<AccountId> account) => new(reader, overview, timeProvider, localization, logger) { AccountFilter = account };
}
