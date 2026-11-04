using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Overview;

/// <summary>One figure in the summary strip, e.g. "4 accounts".</summary>
/// <param name="value">Produces the figure in the current culture.</param>
/// <param name="labelKey">The localisation key of the label.</param>
/// <param name="tone">The colour of the figure.</param>
/// <param name="localization">Supplies the label text.</param>
public sealed class StatTileViewModel(Func<string> value, string labelKey, StatTileTone tone, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The figure, e.g. "4".</summary>
    public string Value => value();

    /// <summary>The label, e.g. "accounts".</summary>
    public string Label => Localization.GetLocal(labelKey);

    /// <summary>The colour of the figure.</summary>
    public StatTileTone Tone => tone;

    /// <summary>Whether the tone is <see cref="StatTileTone.Warn"/>; selects the figure colour.</summary>
    public bool IsWarn => tone is StatTileTone.Warn;

    /// <summary>Whether the tone is <see cref="StatTileTone.Accent"/>; selects the figure colour.</summary>
    public bool IsAccent => tone is StatTileTone.Accent;
}
