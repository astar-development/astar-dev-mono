using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The classification page: a header with the reclassify button above the term list and suggestions tabs.</summary>
/// <param name="tabs">The term list and suggestions tabs.</param>
/// <param name="reclassify">The reclassify button.</param>
/// <param name="localization">Supplies the text and culture.</param>
public sealed class ClassificationPageViewModel(ClassificationTabsViewModel tabs, ReclassifyViewModel reclassify, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>The term list and suggestions tabs.</summary>
    public ClassificationTabsViewModel Tabs { get; } = tabs;

    /// <summary>The reclassify button.</summary>
    public ReclassifyViewModel Reclassify { get; } = reclassify;

    /// <summary>The page heading.</summary>
    public string TitleText => Localization.GetLocal("Classification.Page.Title");

    /// <summary>Reads the term list and checks whether a reclassify is worth recommending; run it each time the page is shown.</summary>
    public IAsyncRelayCommand LoadCommand { get; } = new AsyncRelayCommand(() => Task.WhenAll(tabs.LoadCommand.ExecuteAsync(null), reclassify.CheckCommand.ExecuteAsync(null)));
}
