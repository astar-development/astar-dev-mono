using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>One figure in the summary strip, e.g. "4 accounts".</summary>
/// <param name="value">Produces the figure in the current culture.</param>
/// <param name="labelKey">The localisation key of the label.</param>
/// <param name="localization">Supplies the label text.</param>
public sealed class StatTileViewModel(Func<string> value, string labelKey, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The figure, e.g. "4".</summary>
    public string Value => value();

    /// <summary>The label, e.g. "accounts".</summary>
    public string Label => Localization.GetLocal(labelKey);
}
