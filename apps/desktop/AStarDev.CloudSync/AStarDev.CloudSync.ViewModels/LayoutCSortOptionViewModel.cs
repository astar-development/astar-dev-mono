using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>A choice in layout C's sort menu, e.g. "name".</summary>
/// <param name="order">The sort order this choice applies.</param>
/// <param name="labelKey">The localisation key of the choice text.</param>
/// <param name="localization">Supplies the text and culture.</param>
public sealed class LayoutCSortOptionViewModel(LayoutCSortOrder order, string labelKey, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The sort order this choice applies.</summary>
    public LayoutCSortOrder Order => order;

    /// <summary>The choice text, e.g. "needs attention first".</summary>
    public string Label => Localization.GetLocal(labelKey);
}
