using AStarDev.CloudSync.Core;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>A conflict as shown in a conflict card.</summary>
/// <param name="conflict">The conflict.</param>
/// <param name="syncPairName">The name of the sync pair holding the file.</param>
/// <param name="localization">Supplies the detail and action text.</param>
public sealed class ConflictViewModel(SyncConflict conflict, string syncPairName, ILocalizationService localization) : LocalizedObservableObject(localization)
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
}
