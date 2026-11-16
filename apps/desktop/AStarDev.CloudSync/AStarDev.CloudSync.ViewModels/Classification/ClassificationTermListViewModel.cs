using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Classification;

/// <summary>The classification window's term list: every term with its category, source and file count, filtered as the user types, sortable by column, with an ignore toggle. Terms are never deleted.</summary>
public sealed class ClassificationTermListViewModel : LocalizedObservableObject
{
    private readonly ITermStore termStore;
    private readonly ICategoryStore categoryStore;
    private readonly IFileClassificationStore fileStore;
    private IReadOnlyList<TermRowViewModel> allRows = [];
    private TermListLoadState state = TermListLoadStateFactory.CreateLoading();
    private string filterText = string.Empty;
    private TermSortColumn sortColumn = TermSortColumnFactory.CreateTerm();
    private TermSortDirection sortDirection = TermSortDirectionFactory.CreateAscending();

    /// <summary>Creates the list; it shows nothing until <see cref="LoadCommand"/> runs.</summary>
    /// <param name="termStore">Reads the terms and changes their status.</param>
    /// <param name="categoryStore">Reads the category names.</param>
    /// <param name="fileStore">Reads the file counts.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public ClassificationTermListViewModel(ITermStore termStore, ICategoryStore categoryStore, IFileClassificationStore fileStore, ILocalizationService localization) : base(localization)
    {
        this.termStore = termStore;
        this.categoryStore = categoryStore;
        this.fileStore = fileStore;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SortCommand = new RelayCommand<TermSortColumn>(column => Sort(column!));
        ToggleIgnoredCommand = new AsyncRelayCommand<TermRowViewModel>(ToggleAsync);
    }

    /// <summary>Where loading is.</summary>
    public TermListLoadState State => state;

    /// <summary>The rows that match the filter, in sort order.</summary>
    public IReadOnlyList<TermRowViewModel> Rows { get; private set; } = [];

    /// <summary>The filter text. Rows whose term or category contain it, ignoring case and surrounding spaces, are listed.</summary>
    public string FilterText
    {
        get => filterText;
        set
        {
            if (SetProperty(ref filterText, value ?? string.Empty))
                ApplyView();
        }
    }

    /// <summary>The sort column.</summary>
    public TermSortColumn SortColumn => sortColumn;

    /// <summary>The sort direction.</summary>
    public TermSortDirection SortDirection => sortDirection;

    /// <summary>Whether the terms are being read.</summary>
    public bool IsLoading => state is TermListLoadState.Loading;

    /// <summary>Whether reading failed.</summary>
    public bool IsFailed => state is TermListLoadState.Failed;

    /// <summary>Whether reading worked and there are no terms.</summary>
    public bool IsEmpty => state is TermListLoadState.Loaded && allRows.Count == 0;

    /// <summary>Whether there are terms but the filter hides them all.</summary>
    public bool HasNoMatches => state is TermListLoadState.Loaded && allRows.Count > 0 && Rows.Count == 0;

    /// <summary>The list heading.</summary>
    public string TitleText => Localization.GetLocal("Classification.Terms.Title");

    /// <summary>The filter box placeholder.</summary>
    public string FilterPlaceholderText => Localization.GetLocal("Classification.Terms.FilterPlaceholder");

    /// <summary>Shown while loading.</summary>
    public string LoadingText => Localization.GetLocal("Classification.Terms.Loading");

    /// <summary>Shown when there are no terms.</summary>
    public string EmptyText => Localization.GetLocal("Classification.Terms.Empty");

    /// <summary>Shown when the filter matches nothing.</summary>
    public string NoMatchesText => Localization.GetLocal("Classification.Terms.NoMatches");

    /// <summary>Shown when reading failed.</summary>
    public string LoadFailedText => Localization.GetLocal("Classification.Terms.LoadFailed");

    /// <summary>The term column header.</summary>
    public string TermHeaderText => Localization.GetLocal("Classification.Terms.Column.Term");

    /// <summary>The category column header.</summary>
    public string CategoryHeaderText => Localization.GetLocal("Classification.Terms.Column.Category");

    /// <summary>The source column header.</summary>
    public string SourceHeaderText => Localization.GetLocal("Classification.Terms.Column.Source");

    /// <summary>The file count column header.</summary>
    public string FileCountHeaderText => Localization.GetLocal("Classification.Terms.Column.FileCount");

    /// <summary>The ignored column header.</summary>
    public string IgnoredHeaderText => Localization.GetLocal("Classification.Terms.Column.Ignored");

    /// <summary>Reads the terms, categories and file counts.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Sorts by the column; the same column again flips the direction.</summary>
    public IRelayCommand<TermSortColumn> SortCommand { get; }

    /// <summary>Ignores an active term or restores an ignored one. Never deletes.</summary>
    public IAsyncRelayCommand<TermRowViewModel> ToggleIgnoredCommand { get; }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        SetState(TermListLoadStateFactory.CreateLoading());
        var terms = await termStore.GetAllAsync(cancellationToken).ConfigureAwait(true);
        var categories = await categoryStore.GetAllAsync(cancellationToken).ConfigureAwait(true);
        var counts = await fileStore.GetFileCountsAsync(cancellationToken).ConfigureAwait(true);
        _ = terms.Match(read => categories.Match(named => counts.Match(totals => Show(read, named, totals), NotRead), NotRead), NotRead);
    }

    private Unit Show(IReadOnlyList<ClassificationTerm> terms, IReadOnlyList<ClassificationCategory> categories, IReadOnlyDictionary<TermId, int> counts)
    {
        var names = categories.ToDictionary(category => category.Id, category => category.Name);
        var unknown = Localization.GetLocal("Classification.Terms.UnknownCategory");
        allRows = [.. terms.Select(term => new TermRowViewModel(term, names.GetValueOrDefault(term.CategoryId, unknown), counts.GetValueOrDefault(term.Id), Localization))];
        SetState(TermListLoadStateFactory.CreateLoaded());
        ApplyView();

        return Unit.Instance;
    }

    private Unit NotRead(ClassificationFailure failure)
    {
        allRows = [];
        SetState(TermListLoadStateFactory.CreateFailed());
        ApplyView();

        return Unit.Instance;
    }

    private async Task ToggleAsync(TermRowViewModel? row, CancellationToken cancellationToken)
    {
        if (row is null)
            return;

        var target = row.IsIgnored ? TermStatusFactory.CreateActive() : TermStatusFactory.CreateIgnored();
        var changed = await termStore.SetStatusAsync(row.Id, target, cancellationToken).ConfigureAwait(true);
        _ = changed.Match(updated => Toggled(row, updated), _ => row.Fail());
    }

    private Unit Toggled(TermRowViewModel row, ClassificationTerm updated)
    {
        row.Apply(updated.Status);
        if (sortColumn is TermSortColumn.Ignored)
            ApplyView();

        return Unit.Instance;
    }

    private void Sort(TermSortColumn column)
    {
        sortDirection = column == sortColumn && sortDirection is TermSortDirection.Ascending ? TermSortDirectionFactory.CreateDescending() : TermSortDirectionFactory.CreateAscending();
        sortColumn = column;
        OnPropertyChanged(nameof(SortColumn));
        OnPropertyChanged(nameof(SortDirection));
        ApplyView();
    }

    private void SetState(TermListLoadState next)
    {
        state = next;
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsFailed));
    }

    private void ApplyView()
    {
        var needle = filterText.Trim();
        var matching = allRows.Where(row => needle.Length == 0 || row.Text.Contains(needle, StringComparison.OrdinalIgnoreCase) || row.CategoryName.Contains(needle, StringComparison.OrdinalIgnoreCase));
        Rows = [.. Ordered(matching)];
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoMatches));
    }

    private IOrderedEnumerable<TermRowViewModel> Ordered(IEnumerable<TermRowViewModel> rows)
    {
        var text = StringComparer.Create(Localization.CurrentCulture, true);
        var first = sortColumn switch
        {
            TermSortColumn.Category => OrderBy(rows, row => row.CategoryName, text),
            TermSortColumn.Source => OrderBy(rows, row => row.SourceText, text),
            TermSortColumn.FileCount => OrderBy(rows, row => row.FileCount, Comparer<int>.Default),
            TermSortColumn.Ignored => OrderBy(rows, row => row.IsIgnored, Comparer<bool>.Default),
            _ => OrderBy(rows, row => row.Text, text)
        };

        return first.ThenBy(row => row.Text, text);
    }

    private IOrderedEnumerable<TermRowViewModel> OrderBy<TKey>(IEnumerable<TermRowViewModel> rows, Func<TermRowViewModel, TKey> key, IComparer<TKey> comparer) =>
        sortDirection is TermSortDirection.Descending ? rows.OrderByDescending(key, comparer) : rows.OrderBy(key, comparer);
}
