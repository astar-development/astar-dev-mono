using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>A provider filter chip in layout C, e.g. "OneDrive · 7".</summary>
/// <param name="provider">The provider the chip filters by.</param>
/// <param name="pairCount">How many sync pairs use the provider.</param>
/// <param name="localization">Supplies the label text and culture.</param>
public sealed class ProviderFilterViewModel(ProviderViewModel provider, int pairCount, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The provider the chip filters by; selects the chip's dot colour.</summary>
    public ProviderViewModel Provider => provider;

    /// <summary>The provider name and pair count, e.g. "OneDrive · 7".</summary>
    public string Label => Localization.GetLocal("LayoutC.Filter.Provider", provider.Name, pairCount.ToString("N0", Localization.CurrentCulture));
}
