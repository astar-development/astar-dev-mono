using AStarDev.CloudSync.Sync;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>A conflict as shown in a conflict card.</summary>
/// <param name="conflict">The conflict.</param>
/// <param name="syncPairName">The name of the sync pair holding the file.</param>
/// <param name="syncActions">Resolves the conflict when the user picks an action.</param>
/// <param name="localization">Supplies the detail and action text.</param>
public sealed class ConflictViewModel(SyncConflict conflict, string syncPairName, ISyncActions syncActions, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The conflicting file's name.</summary>
    public string FileName => conflict.FileName;

    /// <summary>Where and why, e.g. "Holiday photos · changed on both sides".</summary>
    public string Detail => Localization.GetLocal("Conflict.ChangedOnBothSides", syncPairName);

    /// <summary>The keep-both action text.</summary>
    public string KeepBothText => Localization.GetLocal("Conflict.KeepBoth");

    /// <summary>The keep-local action text.</summary>
    public string KeepLocalText => Localization.GetLocal("Conflict.KeepLocal");

    /// <summary>The keep-cloud action text.</summary>
    public string KeepCloudText => Localization.GetLocal("Conflict.KeepCloud");

    /// <summary>Resolves the conflict by keeping both copies.</summary>
    public IAsyncRelayCommand KeepBothCommand => field ??= new AsyncRelayCommand(cancellationToken => syncActions.KeepBothAsync(conflict, cancellationToken));

    /// <summary>Resolves the conflict by keeping the local copy.</summary>
    public IAsyncRelayCommand KeepLocalCommand => field ??= new AsyncRelayCommand(cancellationToken => syncActions.KeepLocalAsync(conflict, cancellationToken));

    /// <summary>Resolves the conflict by keeping the cloud copy.</summary>
    public IAsyncRelayCommand KeepCloudCommand => field ??= new AsyncRelayCommand(cancellationToken => syncActions.KeepCloudAsync(conflict, cancellationToken));
}
