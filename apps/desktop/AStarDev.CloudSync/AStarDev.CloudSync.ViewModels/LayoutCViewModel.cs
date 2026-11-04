using System.Windows.Input;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>Layout C ("sync pairs first"): projects the shared overview into a status headline, provider filter chips and a sortable, filterable pair grid.</summary>
public sealed class LayoutCViewModel : LocalizedObservableObject
{
    private Func<SyncPairViewModel, bool> pairFilter = _ => true;
    private LayoutCSortOptionViewModel selectedSortOption;

    /// <summary>Creates the view model and follows the overview's changes.</summary>
    /// <param name="overview">The shared overview every layout binds to.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public LayoutCViewModel(SyncOverviewViewModel overview, ILocalizationService localization) : base(localization)
    {
        Overview = overview;
        SortOptions = [new LayoutCSortOptionViewModel(LayoutCSortOrderFactory.CreateNeedsAttentionFirst(), "LayoutC.Sort.NeedsAttentionFirst", localization), new LayoutCSortOptionViewModel(LayoutCSortOrderFactory.CreateByName(), "LayoutC.Sort.ByName", localization)];
        selectedSortOption = SortOptions[0];
        SelectAllPairsCommand = new RelayCommand(SelectAllPairs);
        overview.PropertyChanged += OnOverviewChanged;
        RebuildProviderFilters();
    }

    /// <summary>The shared overview: accounts, activity, conflicts and throughput.</summary>
    public SyncOverviewViewModel Overview { get; }

    /// <summary>The sync pairs passing the selected filter chip, in the selected sort order.</summary>
    public IReadOnlyList<SyncPairViewModel> SyncPairs => [.. Sort(Overview.SyncPairs.Where(pairFilter))];

    /// <summary>One filter chip per provider in the data, with its pair count.</summary>
    public IReadOnlyList<ProviderFilterViewModel> ProviderFilters { get; private set; } = [];

    /// <summary>Whether the all-pairs chip is the active filter.</summary>
    public bool IsAllPairsSelected => ProviderFilters.All(filter => !filter.IsSelected);

    /// <summary>Makes the all-pairs chip the active filter.</summary>
    public ICommand SelectAllPairsCommand { get; }

    /// <summary>The sort orders the user can choose from.</summary>
    public IReadOnlyList<LayoutCSortOptionViewModel> SortOptions { get; }

    /// <summary>The sort order applied to <see cref="SyncPairs"/>.</summary>
    public LayoutCSortOptionViewModel SelectedSortOption
    {
        get => selectedSortOption;
        set
        {
            if (!SetProperty(ref selectedSortOption, value))
                return;

            OnPropertyChanged(nameof(SortText));
            OnPropertyChanged(nameof(SyncPairs));
        }
    }

    /// <summary>The all-pairs chip, e.g. "All pairs · 7".</summary>
    public string AllPairsFilterText => Localization.GetLocal("LayoutC.Filter.AllPairs", Overview.SyncPairs.Count.ToString("N0", Localization.CurrentCulture));

    /// <summary>The status headline, e.g. "4 in sync, 1 syncing, 2 conflicts".</summary>
    public string Headline => Localization.GetLocal("LayoutC.Headline", Count(pair => pair.Status.IsUpToDate), Count(pair => pair.Status.IsSyncing), ConflictsText);

    /// <summary>The application name shown in the top bar.</summary>
    public string Title => Localization.GetLocal("App.Title");

    /// <summary>The command search placeholder.</summary>
    public string CommandSearchPlaceholder => Localization.GetLocal("LayoutC.CommandSearch.Placeholder");

    /// <summary>The command search keyboard shortcut hint.</summary>
    public string CommandSearchShortcut => Localization.GetLocal("LayoutC.CommandSearch.Shortcut");

    /// <summary>The new sync pair button text.</summary>
    public string NewSyncPairText => Localization.GetLocal("LayoutC.NewSyncPair");

    /// <summary>The label above the status headline.</summary>
    public string StatusLabel => Localization.GetLocal("LayoutC.StatusLabel");

    /// <summary>The selected sort order, e.g. "Sort: needs attention first".</summary>
    public string SortText => Localization.GetLocal("LayoutC.Sort", SelectedSortOption.Label);

    /// <summary>The conflicts panel heading.</summary>
    public string NeedsAttentionText => Localization.GetLocal("LayoutC.NeedsAttention");

    /// <summary>The activity feed heading.</summary>
    public string LiveActivityText => Localization.GetLocal("LayoutC.LiveActivity");

    /// <summary>The live indicator text.</summary>
    public string LiveText => Localization.GetLocal("LayoutC.Live");

    /// <summary>The accounts strip label.</summary>
    public string AccountsText => Localization.GetLocal("LayoutC.Accounts");

    /// <summary>The add account button text.</summary>
    public string AddAccountText => Localization.GetLocal("LayoutC.AddAccount");

    private string ConflictsText => Localization.GetLocal(Overview.Conflicts.Count == 1 ? "Status.Conflicts.One" : "Status.Conflicts.Many", Overview.Conflicts.Count.ToString("N0", Localization.CurrentCulture));

    private string Count(Func<SyncPairViewModel, bool> predicate) => Overview.SyncPairs.Count(predicate).ToString("N0", Localization.CurrentCulture);

    private static int AttentionRank(SyncPairViewModel pair)
    {
        if (pair.Status.HasConflicts)
            return 0;

        return pair.Status.IsSyncing ? 1 : 2;
    }

    private IEnumerable<SyncPairViewModel> Sort(IEnumerable<SyncPairViewModel> pairs) => SelectedSortOption.Order switch
    {
        LayoutCSortOrder.ByName => pairs.OrderBy(pair => pair.Name, StringComparer.Create(Localization.CurrentCulture, ignoreCase: true)),
        _ => pairs.OrderBy(AttentionRank)
    };

    private void OnOverviewChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SyncOverviewViewModel.SyncPairs))
            RebuildProviderFilters();

        OnPropertyChanged(nameof(SyncPairs));
        OnPropertyChanged(nameof(ProviderFilters));
        OnPropertyChanged(nameof(AllPairsFilterText));
        OnPropertyChanged(nameof(Headline));
    }

    private void RebuildProviderFilters()
    {
        ProviderFilters = [.. Overview.SyncPairs.GroupBy(pair => pair.Provider.Name, StringComparer.Ordinal).Select(group => new ProviderFilterViewModel(group.First().Provider, group.Count(), SelectProvider, Localization))];
        SelectAllPairs();
    }

    private void SelectAllPairs() => ApplyFilter(_ => true, _ => false);

    private void SelectProvider(ProviderFilterViewModel selected) => ApplyFilter(selected.Matches, filter => ReferenceEquals(filter, selected));

    private void ApplyFilter(Func<SyncPairViewModel, bool> matches, Func<ProviderFilterViewModel, bool> isSelected)
    {
        foreach (var filter in ProviderFilters)
            filter.IsSelected = isSelected(filter);

        pairFilter = matches;
        OnPropertyChanged(nameof(IsAllPairsSelected));
        OnPropertyChanged(nameof(SyncPairs));
    }
}
