using AStarDev.CloudSync.Classification;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The classification window's suggestions list.</summary>
public sealed class ClassificationSuggestionsViewModel : LocalizedObservableObject
{
    /// <summary>Creates the list.</summary>
    /// <param name="suggestions">Scans, promotes and ignores.</param>
    /// <param name="categoryStore">Reads the categories.</param>
    /// <param name="localization">Supplies the text.</param>
    public ClassificationSuggestionsViewModel(IClassificationSuggestions suggestions, ICategoryStore categoryStore, ILocalizationService localization) : base(localization) => throw new NotImplementedException($"{nameof(suggestions)} {nameof(categoryStore)}");

    /// <summary>Raised after a word was promoted or ignored.</summary>
    public event EventHandler? TermsChanged { add => throw new NotImplementedException(); remove => throw new NotImplementedException(); }

    /// <summary>Where the scan is.</summary>
    public SuggestionsLoadState State => throw new NotImplementedException();

    /// <summary>The rows that match the filter, in rank order.</summary>
    public IReadOnlyList<SuggestionRowViewModel> Rows => throw new NotImplementedException();

    /// <summary>The categories that match the picker filter, in store order.</summary>
    public IReadOnlyList<ClassificationCategory> Categories => throw new NotImplementedException();

    /// <summary>The filter text. Rows whose word contains it, ignoring case and surrounding spaces, are listed.</summary>
    public string FilterText { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary>The picker filter text. Categories whose name contains it, ignoring case and surrounding spaces, are listed.</summary>
    public string CategoryFilterText { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary>Whether the scan is running.</summary>
    public bool IsLoading => throw new NotImplementedException();

    /// <summary>Whether the scan failed.</summary>
    public bool IsFailed => throw new NotImplementedException();

    /// <summary>Whether the user cancelled the scan.</summary>
    public bool IsCancelled => throw new NotImplementedException();

    /// <summary>Whether the scan worked and found nothing.</summary>
    public bool IsEmpty => throw new NotImplementedException();

    /// <summary>Whether there are suggestions but the filter hides them all.</summary>
    public bool HasNoMatches => throw new NotImplementedException();

    /// <summary>Whether the category picker is open.</summary>
    public bool IsPickerOpen => throw new NotImplementedException();

    /// <summary>Whether the categories could not be read.</summary>
    public bool IsCategoriesFailed => throw new NotImplementedException();

    /// <summary>Whether the categories were read and there are none.</summary>
    public bool HasNoCategories => throw new NotImplementedException();

    /// <summary>Whether there are categories but the picker filter hides them all.</summary>
    public bool HasNoCategoryMatches => throw new NotImplementedException();

    /// <summary>The action the picker is choosing a category for.</summary>
    public SuggestionAction PickerAction => throw new NotImplementedException();

    /// <summary>The picker heading, naming the word and the action.</summary>
    public string PickerTitleText => throw new NotImplementedException();

    /// <summary>The list heading, also used as the tab header.</summary>
    public string TitleText => throw new NotImplementedException();

    /// <summary>The filter box placeholder.</summary>
    public string FilterPlaceholderText => throw new NotImplementedException();

    /// <summary>Shown while scanning.</summary>
    public string LoadingText => throw new NotImplementedException();

    /// <summary>Shown when there are no suggestions.</summary>
    public string EmptyText => throw new NotImplementedException();

    /// <summary>Shown when the filter matches nothing.</summary>
    public string NoMatchesText => throw new NotImplementedException();

    /// <summary>Shown when the scan failed.</summary>
    public string LoadFailedText => throw new NotImplementedException();

    /// <summary>Shown when the user cancelled the scan.</summary>
    public string CancelledText => throw new NotImplementedException();

    /// <summary>The label of the cancel-scan button.</summary>
    public string CancelText => throw new NotImplementedException();

    /// <summary>The label of the rescan button.</summary>
    public string RefreshText => throw new NotImplementedException();

    /// <summary>The word column header.</summary>
    public string WordHeaderText => throw new NotImplementedException();

    /// <summary>The file count column header.</summary>
    public string FileCountHeaderText => throw new NotImplementedException();

    /// <summary>The picker filter placeholder.</summary>
    public string CategoryFilterPlaceholderText => throw new NotImplementedException();

    /// <summary>Shown when there are no categories.</summary>
    public string NoCategoriesText => throw new NotImplementedException();

    /// <summary>Shown when the picker filter matches nothing.</summary>
    public string NoCategoryMatchesText => throw new NotImplementedException();

    /// <summary>Shown when the categories could not be read.</summary>
    public string CategoriesFailedText => throw new NotImplementedException();

    /// <summary>The label of the picker close button.</summary>
    public string ClosePickerText => throw new NotImplementedException();

    /// <summary>Scans the file paths for suggestions. Also the initial load.</summary>
    public IAsyncRelayCommand LoadCommand => throw new NotImplementedException();

    /// <summary>Scans the file paths again, replacing the held result.</summary>
    public IAsyncRelayCommand RefreshCommand => throw new NotImplementedException();

    /// <summary>Cancels a running scan. Cancelling is a normal outcome.</summary>
    public IRelayCommand CancelCommand => throw new NotImplementedException();

    /// <summary>Opens the picker to promote the row's word.</summary>
    public IAsyncRelayCommand<SuggestionRowViewModel> PromoteCommand => throw new NotImplementedException();

    /// <summary>Opens the picker to ignore the row's word.</summary>
    public IAsyncRelayCommand<SuggestionRowViewModel> IgnoreCommand => throw new NotImplementedException();

    /// <summary>Applies the open picker's action with the chosen category.</summary>
    public IAsyncRelayCommand<ClassificationCategory> ChooseCategoryCommand => throw new NotImplementedException();

    /// <summary>Closes the picker without changing anything.</summary>
    public IRelayCommand ClosePickerCommand => throw new NotImplementedException();
}
