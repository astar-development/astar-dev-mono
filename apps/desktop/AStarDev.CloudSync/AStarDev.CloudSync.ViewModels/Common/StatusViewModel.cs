using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Common;

/// <summary>A sync status as shown in a status pill.</summary>
/// <param name="status">The status.</param>
/// <param name="localization">Supplies the status text.</param>
public sealed class StatusViewModel(SyncStatus status, AccountSignInState signIn, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>Shows a sync status for something with no sign-in of its own, such as a sync pair.</summary>
    /// <param name="status">The sync status.</param>
    /// <param name="localization">Supplies the text.</param>
    public StatusViewModel(SyncStatus status, ILocalizationService localization) : this(status, AccountSignInStateFactory.CreateSignedIn(), localization)
    {
    }

    /// <summary>Whether the account must sign in again before it can sync; this wins over the sync status.</summary>
    public bool IsSignInRequired => signIn is AccountSignInState.SignInRequired;

    /// <summary>The status text, e.g. "Syncing 25%" or "2 conflicts".</summary>
    public string Text => IsSignInRequired ? Localization.GetLocal("Status.SignInRequired") : SyncText;

    private string SyncText => status switch
    {
        SyncStatus.Syncing syncing => Localization.GetLocal("Status.Syncing", syncing.PercentComplete),
        SyncStatus.HasConflicts { Count: 1 } => Localization.GetLocal("Status.Conflicts.One", 1),
        SyncStatus.HasConflicts conflicts => Localization.GetLocal("Status.Conflicts.Many", conflicts.Count),
        SyncStatus.Paused => Localization.GetLocal("Status.Paused"),
        _ => Localization.GetLocal("Status.UpToDate")
    };

    /// <summary>Whether syncing; selects the pill colour.</summary>
    public bool IsSyncing => !IsSignInRequired && status is SyncStatus.Syncing;

    /// <summary>Whether up to date; selects the pill colour.</summary>
    public bool IsUpToDate => !IsSignInRequired && status is SyncStatus.UpToDate;

    /// <summary>Whether there are conflicts; selects the pill colour.</summary>
    public bool HasConflicts => !IsSignInRequired && status is SyncStatus.HasConflicts;

    /// <summary>Whether paused; selects the pill colour.</summary>
    public bool IsPaused => !IsSignInRequired && status is SyncStatus.Paused;

    /// <summary>Sync progress from 0 to 1: the percentage when syncing, 0 when paused (no progress is known), otherwise 1.</summary>
    public double Progress => status switch
    {
        SyncStatus.Syncing syncing => syncing.PercentComplete / 100d,
        SyncStatus.Paused => 0,
        _ => 1
    };
}
