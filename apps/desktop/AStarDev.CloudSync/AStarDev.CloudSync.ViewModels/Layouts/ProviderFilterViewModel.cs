using System.Windows.Input;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>A provider filter chip in layout C, e.g. "OneDrive · 7".</summary>
public sealed class ProviderFilterViewModel : LocalizedObservableObject
{
    private readonly int pairCount;
    private bool isSelected;

    /// <summary>Creates the chip.</summary>
    /// <param name="provider">The provider the chip filters by.</param>
    /// <param name="pairCount">How many sync pairs use the provider.</param>
    /// <param name="select">Called with this chip when the user selects it.</param>
    /// <param name="localization">Supplies the label text and culture.</param>
    public ProviderFilterViewModel(ProviderViewModel provider, int pairCount, Action<ProviderFilterViewModel> select, ILocalizationService localization) : base(localization)
    {
        Provider = provider;
        this.pairCount = pairCount;
        SelectCommand = new RelayCommand(() => select(this));
    }

    /// <summary>The provider the chip filters by; selects the chip's dot colour.</summary>
    public ProviderViewModel Provider { get; }

    /// <summary>The provider name and pair count, e.g. "OneDrive · 7".</summary>
    public string Label => Localization.GetLocal("LayoutC.Filter.Provider", Provider.Name, pairCount.ToString("N0", Localization.CurrentCulture));

    /// <summary>Whether this chip is the active filter.</summary>
    public bool IsSelected
    {
        get => isSelected;
        internal set => SetProperty(ref isSelected, value);
    }

    /// <summary>Makes this chip the active filter.</summary>
    public ICommand SelectCommand { get; }

    /// <summary>Whether <paramref name="pair"/> passes this chip's filter.</summary>
    /// <param name="pair">The sync pair to test.</param>
    /// <returns><see langword="true"/> when the pair's account uses this chip's provider.</returns>
    public bool Matches(SyncPairViewModel pair) => string.Equals(pair.Provider.Name, Provider.Name, StringComparison.Ordinal);
}
