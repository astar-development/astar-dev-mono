using AStarDev.CloudSync.Core;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>A sync status as shown in a status pill.</summary>
/// <param name="status">The status.</param>
/// <param name="localization">Supplies the status text.</param>
public sealed class StatusViewModel(SyncStatus status, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The status text, e.g. "Syncing 25%" or "2 conflicts".</summary>
    public string Text => status switch
    {
        SyncStatus.Syncing syncing => Localization.GetLocal("Status.Syncing", syncing.PercentComplete),
        SyncStatus.HasConflicts { Count: 1 } => Localization.GetLocal("Status.Conflicts.One", 1),
        SyncStatus.HasConflicts conflicts => Localization.GetLocal("Status.Conflicts.Many", conflicts.Count),
        SyncStatus.Paused => Localization.GetLocal("Status.Paused"),
        _ => Localization.GetLocal("Status.UpToDate")
    };

    /// <summary>Whether syncing; selects the pill colour.</summary>
    public bool IsSyncing => status is SyncStatus.Syncing;

    /// <summary>Whether up to date; selects the pill colour.</summary>
    public bool IsUpToDate => status is SyncStatus.UpToDate;

    /// <summary>Whether there are conflicts; selects the pill colour.</summary>
    public bool HasConflicts => status is SyncStatus.HasConflicts;

    /// <summary>Whether paused; selects the pill colour.</summary>
    public bool IsPaused => status is SyncStatus.Paused;
}
