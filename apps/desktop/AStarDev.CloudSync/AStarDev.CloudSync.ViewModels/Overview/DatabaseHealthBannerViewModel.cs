using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Overview;

/// <summary>The banner that tells the user syncing is stopped because the database failed its integrity check (#1612).</summary>
/// <param name="gate">Holds the answer of the database health check.</param>
/// <param name="dispatcher">Runs the change announcement on the UI thread.</param>
/// <param name="localization">Supplies the text.</param>
public sealed class DatabaseHealthBannerViewModel(IDatabaseHealthGate gate, IUiDispatcher dispatcher, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The plain-language message; empty unless the check failed.</summary>
    public string Text => throw new NotImplementedException($"{gate} {dispatcher}");
}
