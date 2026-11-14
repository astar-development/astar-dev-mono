using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Overview;

/// <summary>The banner that tells the user syncing is stopped because the database failed its integrity check (#1612).</summary>
public sealed class DatabaseHealthBannerViewModel : LocalizedObservableObject
{
    private readonly IDatabaseHealthGate gate;

    /// <summary>Starts following the health check.</summary>
    /// <param name="gate">Holds the answer of the database health check.</param>
    /// <param name="dispatcher">Runs the change announcement on the UI thread.</param>
    /// <param name="localization">Supplies the text.</param>
    public DatabaseHealthBannerViewModel(IDatabaseHealthGate gate, IUiDispatcher dispatcher, ILocalizationService localization) : base(localization)
    {
        this.gate = gate;
        gate.Changed += (_, _) => _ = dispatcher.InvokeAsync(() => OnPropertyChanged(nameof(Text)));
    }

    /// <summary>The plain-language message; empty unless the check failed.</summary>
    public string Text => gate.Status is DatabaseHealthStatus.Failed failed ? Localization.GetLocal(KeyFor(failed.Failure)) : string.Empty;

    private static string KeyFor(DatabaseHealthFailure failure) => failure is DatabaseHealthFailure.Damaged ? "Health.DatabaseDamaged" : "Health.DatabaseUnreadable";
}
