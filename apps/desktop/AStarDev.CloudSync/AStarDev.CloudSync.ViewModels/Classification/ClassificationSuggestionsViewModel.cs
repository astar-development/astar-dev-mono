using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The classification window's suggestions list: words found in file paths that are not yet terms, ranked by file count, filtered as the user types. The scan result is held here between promote and ignore actions; only <see cref="RefreshCommand"/> rescans.</summary>
public sealed class ClassificationSuggestionsViewModel : LocalizedObservableObject
{
    private readonly IClassificationSuggestions suggestions;
    private readonly ICategoryStore categoryStore;
    private IReadOnlyList<SuggestionRowViewModel> allRows = [];
    private IReadOnlyList<ClassificationCategory> allCategories = [];
    private SuggestionsLoadState state = SuggestionsLoadStateFactory.CreateLoading();
    private SuggestionsLoadState categoryState = SuggestionsLoadStateFactory.CreateLoading();
    private SuggestionAction pickerAction = SuggestionActionFactory.CreatePromote();
    private SuggestionRowViewModel? pickerRow;
    private string filterText = string.Empty;
    private string categoryFilterText = string.Empty;

    /// <summary>Creates the list; it shows nothing until <see cref="LoadCommand"/> runs.</summary>
    /// <param name="suggestions">Scans for words and promotes or ignores them.</param>
    /// <param name="categoryStore">Reads the categories for the picker.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public ClassificationSuggestionsViewModel(IClassificationSuggestions suggestions, ICategoryStore categoryStore, ILocalizationService localization) : base(localization)
    {
        this.suggestions = suggestions;
        this.categoryStore = categoryStore;
        LoadCommand = new AsyncRelayCommand(ScanAsync);
        CancelCommand = new RelayCommand(LoadCommand.Cancel, () => IsLoading);
        PromoteCommand = new AsyncRelayCommand<SuggestionRowViewModel>(row => OpenPickerAsync(row, SuggestionActionFactory.CreatePromote()));
        IgnoreCommand = new AsyncRelayCommand<SuggestionRowViewModel>(row => OpenPickerAsync(row, SuggestionActionFactory.CreateIgnore()));
        ChooseCategoryCommand = new AsyncRelayCommand<ClassificationCategory>(ChooseAsync);
        ClosePickerCommand = new RelayCommand(ClosePicker);
    }

    /// <summary>Raised after a word was promoted or ignored, so a host can reload the term list.</summary>
    public event EventHandler? TermsChanged;

    /// <summary>Where the scan is.</summary>
    public SuggestionsLoadState State => state;

    /// <summary>The rows that match the filter, in rank order.</summary>
    public IReadOnlyList<SuggestionRowViewModel> Rows { get; private set; } = [];

    /// <summary>The categories that match the picker filter, in store order.</summary>
    public IReadOnlyList<ClassificationCategory> Categories { get; private set; } = [];

    /// <summary>The filter text. Rows whose word contains it, ignoring case and surrounding spaces, are listed.</summary>
    public string FilterText
    {
        get => filterText;
        set
        {
            if (SetProperty(ref filterText, value ?? string.Empty))
                ApplyView();
        }
    }

    /// <summary>The picker filter text. Categories whose name contains it, ignoring case and surrounding spaces, are listed.</summary>
    public string CategoryFilterText
    {
        get => categoryFilterText;
        set
        {
            if (SetProperty(ref categoryFilterText, value ?? string.Empty))
                ApplyCategoryView();
        }
    }

    /// <summary>Whether the scan is running.</summary>
    public bool IsLoading => state is SuggestionsLoadState.Loading;

    /// <summary>Whether the scan failed.</summary>
    public bool IsFailed => state is SuggestionsLoadState.Failed;

    /// <summary>Whether the user cancelled the scan.</summary>
    public bool IsCancelled => state is SuggestionsLoadState.Cancelled;

    /// <summary>Whether the scan worked and found nothing.</summary>
    public bool IsEmpty => state is SuggestionsLoadState.Loaded && allRows.Count == 0;

    /// <summary>Whether there are suggestions but the filter hides them all.</summary>
    public bool HasNoMatches => state is SuggestionsLoadState.Loaded && allRows.Count > 0 && Rows.Count == 0;

    /// <summary>Whether the category picker is open.</summary>
    public bool IsPickerOpen => pickerRow is not null;

    /// <summary>Whether the categories could not be read.</summary>
    public bool IsCategoriesFailed => IsPickerOpen && categoryState is SuggestionsLoadState.Failed;

    /// <summary>Whether the categories were read and there are none.</summary>
    public bool HasNoCategories => IsPickerOpen && categoryState is SuggestionsLoadState.Loaded && allCategories.Count == 0;

    /// <summary>Whether there are categories but the picker filter hides them all.</summary>
    public bool HasNoCategoryMatches => IsPickerOpen && categoryState is SuggestionsLoadState.Loaded && allCategories.Count > 0 && Categories.Count == 0;

    /// <summary>The action the picker is choosing a category for.</summary>
    public SuggestionAction PickerAction => pickerAction;

    /// <summary>The picker heading, naming the word and the action.</summary>
    public string PickerTitleText => pickerRow is null ? string.Empty : Localization.GetLocal(PickerTitleKey(), pickerRow.Word);

    /// <summary>The list heading, also used as the tab header.</summary>
    public string TitleText => Localization.GetLocal("Classification.Suggestions.Title");

    /// <summary>The filter box placeholder.</summary>
    public string FilterPlaceholderText => Localization.GetLocal("Classification.Suggestions.FilterPlaceholder");

    /// <summary>Shown while scanning.</summary>
    public string LoadingText => Localization.GetLocal("Classification.Suggestions.Loading");

    /// <summary>Shown when there are no suggestions.</summary>
    public string EmptyText => Localization.GetLocal("Classification.Suggestions.Empty");

    /// <summary>Shown when the filter matches nothing.</summary>
    public string NoMatchesText => Localization.GetLocal("Classification.Suggestions.NoMatches");

    /// <summary>Shown when the scan failed.</summary>
    public string LoadFailedText => Localization.GetLocal("Classification.Suggestions.LoadFailed");

    /// <summary>Shown when the user cancelled the scan.</summary>
    public string CancelledText => Localization.GetLocal("Classification.Suggestions.Cancelled");

    /// <summary>The label of the cancel-scan button.</summary>
    public string CancelText => Localization.GetLocal("Classification.Suggestions.Cancel");

    /// <summary>The label of the rescan button.</summary>
    public string RefreshText => Localization.GetLocal("Classification.Suggestions.Refresh");

    /// <summary>The word column header.</summary>
    public string WordHeaderText => Localization.GetLocal("Classification.Suggestions.Column.Word");

    /// <summary>The file count column header.</summary>
    public string FileCountHeaderText => Localization.GetLocal("Classification.Suggestions.Column.FileCount");

    /// <summary>The picker filter placeholder.</summary>
    public string CategoryFilterPlaceholderText => Localization.GetLocal("Classification.Suggestions.Picker.FilterPlaceholder");

    /// <summary>Shown when there are no categories.</summary>
    public string NoCategoriesText => Localization.GetLocal("Classification.Suggestions.Picker.Empty");

    /// <summary>Shown when the picker filter matches nothing.</summary>
    public string NoCategoryMatchesText => Localization.GetLocal("Classification.Suggestions.Picker.NoMatches");

    /// <summary>Shown when the categories could not be read.</summary>
    public string CategoriesFailedText => Localization.GetLocal("Classification.Suggestions.Picker.LoadFailed");

    /// <summary>The label of the picker close button.</summary>
    public string ClosePickerText => Localization.GetLocal("Classification.Suggestions.Picker.Close");

    /// <summary>Scans the file paths for suggestions. Also the initial load.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Scans the file paths again, replacing the held result.</summary>
    public IAsyncRelayCommand RefreshCommand => LoadCommand;

    /// <summary>Cancels a running scan. Cancelling is a normal outcome.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Opens the picker to promote the row's word.</summary>
    public IAsyncRelayCommand<SuggestionRowViewModel> PromoteCommand { get; }

    /// <summary>Opens the picker to ignore the row's word.</summary>
    public IAsyncRelayCommand<SuggestionRowViewModel> IgnoreCommand { get; }

    /// <summary>Applies the open picker's action with the chosen category.</summary>
    public IAsyncRelayCommand<ClassificationCategory> ChooseCategoryCommand { get; }

    /// <summary>Closes the picker without changing anything.</summary>
    public IRelayCommand ClosePickerCommand { get; }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        allRows = [];
        SetState(SuggestionsLoadStateFactory.CreateLoading());
        ApplyView();
        var scan = suggestions.GetSuggestionsAsync(cancellationToken);
        await ((Task)scan).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        _ = Finish(scan);
    }

    private Unit Finish(Task<Result<IReadOnlyList<ClassificationSuggestion>, ClassificationFailure>> scan)
    {
        if (scan.IsCanceled)
            return Settle(SuggestionsLoadStateFactory.CreateCancelled(), []);

        if (scan.IsFaulted)
            return Settle(SuggestionsLoadStateFactory.CreateFailed(), []);

        return scan.Result.Match(found => Settle(SuggestionsLoadStateFactory.CreateLoaded(), [.. found.Select(suggestion => new SuggestionRowViewModel(suggestion, Localization))]), _ => Settle(SuggestionsLoadStateFactory.CreateFailed(), []));
    }

    private Unit Settle(SuggestionsLoadState next, IReadOnlyList<SuggestionRowViewModel> rows)
    {
        allRows = rows;
        SetState(next);
        ApplyView();

        return Unit.Instance;
    }

    private async Task OpenPickerAsync(SuggestionRowViewModel? row, SuggestionAction action)
    {
        if (row is null)
            return;

        row.ClearError();
        pickerAction = action;
        pickerRow = row;
        categoryFilterText = string.Empty;
        categoryState = SuggestionsLoadStateFactory.CreateLoading();
        NotifyPicker();
        OnPropertyChanged(nameof(CategoryFilterText));
        var read = await categoryStore.GetAllAsync(CancellationToken.None).ConfigureAwait(true);
        _ = read.Match(named => ShowCategories(named), _ => CategoriesNotRead());
    }

    private Unit ShowCategories(IReadOnlyList<ClassificationCategory> categories)
    {
        allCategories = categories;
        categoryState = SuggestionsLoadStateFactory.CreateLoaded();
        ApplyCategoryView();

        return Unit.Instance;
    }

    private Unit CategoriesNotRead()
    {
        allCategories = [];
        categoryState = SuggestionsLoadStateFactory.CreateFailed();
        ApplyCategoryView();

        return Unit.Instance;
    }

    private async Task ChooseAsync(ClassificationCategory? category, CancellationToken cancellationToken)
    {
        var row = pickerRow;
        if (row is null || category is null)
            return;

        var applied = await ApplyAsync(row, category, cancellationToken).ConfigureAwait(true);
        _ = applied.Match(_ => Resolved(row), _ => Unresolved(row));
    }

    private Task<Result<ClassificationTerm, ClassificationFailure>> ApplyAsync(SuggestionRowViewModel row, ClassificationCategory category, CancellationToken cancellationToken) =>
        pickerAction is SuggestionAction.Ignore ? suggestions.IgnoreAsync(row.Word, category.Id, cancellationToken) : suggestions.PromoteAsync(row.Word, category.Id, cancellationToken);

    private Unit Resolved(SuggestionRowViewModel row)
    {
        allRows = [.. allRows.Where(candidate => candidate != row)];
        ClosePicker();
        ApplyView();
        TermsChanged?.Invoke(this, EventArgs.Empty);

        return Unit.Instance;
    }

    private Unit Unresolved(SuggestionRowViewModel row)
    {
        ClosePicker();

        return row.Fail();
    }

    private void ClosePicker()
    {
        pickerRow = null;
        allCategories = [];
        Categories = [];
        NotifyPicker();
        OnPropertyChanged(nameof(Categories));
    }

    private string PickerTitleKey() => pickerAction is SuggestionAction.Ignore ? "Classification.Suggestions.Picker.IgnoreTitle" : "Classification.Suggestions.Picker.PromoteTitle";

    private void NotifyPicker()
    {
        OnPropertyChanged(nameof(IsPickerOpen));
        OnPropertyChanged(nameof(PickerAction));
        OnPropertyChanged(nameof(PickerTitleText));
        OnPropertyChanged(nameof(IsCategoriesFailed));
        OnPropertyChanged(nameof(HasNoCategories));
        OnPropertyChanged(nameof(HasNoCategoryMatches));
    }

    private void SetState(SuggestionsLoadState next)
    {
        state = next;
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsFailed));
        OnPropertyChanged(nameof(IsCancelled));
        CancelCommand.NotifyCanExecuteChanged();
    }

    private void ApplyView()
    {
        var needle = filterText.Trim();
        Rows = [.. allRows.Where(row => needle.Length == 0 || row.Word.Contains(needle, StringComparison.OrdinalIgnoreCase))];
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoMatches));
    }

    private void ApplyCategoryView()
    {
        var needle = categoryFilterText.Trim();
        Categories = [.. allCategories.Where(category => needle.Length == 0 || category.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))];
        OnPropertyChanged(nameof(Categories));
        NotifyPicker();
    }
}
