using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Activity;

/// <summary>One activity entry as shown in an activity row.</summary>
/// <param name="entry">The activity entry.</param>
/// <param name="accountName">The display name of the entry's account.</param>
/// <param name="provider">The provider of the entry's account.</param>
/// <param name="localization">Supplies the activity text.</param>
/// <param name="timeProvider">Supplies the current time for the relative time.</param>
public sealed class ActivityItemViewModel(ActivityEntry entry, string accountName, ProviderViewModel provider, ILocalizationService localization, TimeProvider timeProvider) : LocalizedObservableObject(localization)
{
    /// <summary>The id of the entry's account.</summary>
    public AccountId AccountId => entry.AccountId;

    /// <summary>The provider of the entry's account.</summary>
    public ProviderViewModel Provider => provider;

    /// <summary>What happened, e.g. "Uploaded 12 files (84.0 MB)".</summary>
    public string Text => entry.Kind switch
    {
        ActivityKind.DetectingChanges => Localization.GetLocal("Activity.DetectingChanges"),
        ActivityKind.Uploaded { FileCount: 1 } uploaded => Localization.GetLocal("Activity.Uploaded.One", ByteSizeFormatter.Format(uploaded.Bytes, Localization)),
        ActivityKind.Uploaded uploaded => Localization.GetLocal("Activity.Uploaded.Many", uploaded.FileCount.ToString("N0", Localization.CurrentCulture), ByteSizeFormatter.Format(uploaded.Bytes, Localization)),
        ActivityKind.Downloaded { FileCount: 1 } => Localization.GetLocal("Activity.Downloaded.One"),
        ActivityKind.Downloaded downloaded => Localization.GetLocal("Activity.Downloaded.Many", downloaded.FileCount.ToString("N0", Localization.CurrentCulture)),
        ActivityKind.Deleted { FileCount: 1 } => Localization.GetLocal("Activity.Deleted.One"),
        ActivityKind.Deleted deleted => Localization.GetLocal("Activity.Deleted.Many", deleted.FileCount.ToString("N0", Localization.CurrentCulture)),
        ActivityKind.ConflictDetected conflict => Localization.GetLocal("Activity.Conflict", conflict.FileName),
        ActivityKind.SyncInterrupted => throw new NotImplementedException(accountName),
        _ => Localization.GetLocal("Activity.Authenticated")
    };

    /// <summary>When it happened, relative to now, e.g. "5 min ago".</summary>
    public string WhenText => RelativeTimeFormatter.Format(entry.OccurredAt, Localization, timeProvider);
}
