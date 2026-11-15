using System.Windows.Input;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.Search;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>Layout C ("sync pairs first"): projects the shared overview into a status headline, provider filter chips and a sortable, filterable pair grid.</summary>
public sealed class LayoutCViewModel : LocalizedObservableObject
{
    private readonly ISyncActions syncActions;
    private Func<SyncPairViewModel, bool> pairFilter = _ => true;
    private LayoutCSortOptionViewModel selectedSortOption;
    private Option<AccountId> selectedAccountId = Option.None<AccountId>();
    private IReadOnlyList<LayoutCAccountCardViewModel> allAccountCards = [];
    private string searchText = string.Empty;

    /// <summary>Creates the view model and follows the overview's changes.</summary>
    /// <param name="overview">The shared overview every layout binds to.</param>
    /// <param name="syncActions">Carries out the account and sync pair actions the user picks.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public LayoutCViewModel(SyncOverviewViewModel overview, ISyncActions syncActions, ILocalizationService localization) : base(localization)
    {
        Overview = overview;
        this.syncActions = syncActions;
        SortOptions = [new LayoutCSortOptionViewModel(LayoutCSortOrderFactory.CreateNeedsAttentionFirst(), "LayoutC.Sort.NeedsAttentionFirst", localization), new LayoutCSortOptionViewModel(LayoutCSortOrderFactory.CreateByName(), "LayoutC.Sort.ByName", localization)];
        selectedSortOption = SortOptions[0];
        SelectAllPairsCommand = new RelayCommand(SelectAllPairs);
        NewSyncPairCommand = new AsyncRelayCommand(cancellationToken => syncActions.AddSyncPairAsync(Option.None<AccountId>(), cancellationToken));
        AddAccountCommand = new AsyncRelayCommand(AddAccountAndReloadAsync);
        overview.PropertyChanged += OnOverviewChanged;
        RebuildProviderFilters();
        RebuildAccountCards();
    }

    /// <summary>The shared overview: accounts, activity, conflicts and throughput.</summary>
    public SyncOverviewViewModel Overview { get; }

    /// <summary>The sync pairs passing the selected filter chip and the selected account, in the selected sort order.</summary>
    public IReadOnlyList<SyncPairViewModel> SyncPairs => [.. Sort(Overview.SyncPairs.Where(pairFilter).Where(IsOfSelectedAccount).Where(IsShownBySearch))];

    /// <summary>What the user typed in the command search box. Pairs whose name or path match, or whose account's name, email or provider match, are shown, with the cards of those accounts; blank shows everything.</summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (!SetProperty(ref searchText, value ?? string.Empty))
                return;

            ShowSearchedAccountCards();
            OnPropertyChanged(nameof(SyncPairs));
        }
    }

    /// <summary>One card per account in the accounts strip that passes the search; selecting one shows only that account's sync pairs.</summary>
    public IReadOnlyList<LayoutCAccountCardViewModel> AccountCards { get; private set; } = [];

    /// <summary>One filter chip per provider in the data, with its pair count.</summary>
    public IReadOnlyList<ProviderFilterViewModel> ProviderFilters { get; private set; } = [];

    /// <summary>Whether the all-pairs chip is the active filter.</summary>
    public bool IsAllPairsSelected => ProviderFilters.All(filter => !filter.IsSelected) && selectedAccountId.Match(_ => false, () => true);

    /// <summary>Makes the all-pairs chip the active filter, clearing the provider chip and the selected account.</summary>
    public ICommand SelectAllPairsCommand { get; }

    /// <summary>Starts adding a sync pair, letting the user choose the account.</summary>
    public IAsyncRelayCommand NewSyncPairCommand { get; }

    /// <summary>Starts connecting a new account.</summary>
    public IAsyncRelayCommand AddAccountCommand { get; }

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

    /// <summary>The status headline, e.g. "4 in sync, 1 syncing, 2 conflicts, 1 paused".</summary>
    public string Headline => Localization.GetLocal("LayoutC.Headline", Count(pair => pair.Status.IsUpToDate), Count(pair => pair.Status.IsSyncing), ConflictsText, Count(pair => pair.Status.IsPaused));

    /// <summary>The data moved today, e.g. "3.1 GB moved today".</summary>
    public string MovedTodayText => Localization.GetLocal("LayoutC.MovedToday", ByteSizeFormatter.Format(Overview.BytesMovedToday, Localization));

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

    /// <summary>The activity feed heading.</summary>
    public string LiveActivityText => Localization.GetLocal("LayoutC.LiveActivity");

    /// <summary>The live indicator text.</summary>
    public string LiveText => Localization.GetLocal("LayoutC.Live");

    /// <summary>The accounts strip label.</summary>
    public string AccountsText => Localization.GetLocal("LayoutC.Accounts");

    /// <summary>The add account button text.</summary>
    public string AddAccountText => Localization.GetLocal("LayoutC.AddAccount");

    private string ConflictsText => Localization.GetLocal(CountKey.For("Status.Conflicts", Overview.ConflictCount), Overview.ConflictCount.ToString("N0", Localization.CurrentCulture));

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

        if (args.PropertyName == nameof(SyncOverviewViewModel.Accounts))
            RebuildAccountCards();

        OnPropertyChanged(nameof(SyncPairs));
        OnPropertyChanged(nameof(ProviderFilters));
        OnPropertyChanged(nameof(AllPairsFilterText));
        OnPropertyChanged(nameof(Headline));
        OnPropertyChanged(nameof(MovedTodayText));
    }

    private void RebuildProviderFilters()
    {
        ProviderFilters = [.. Overview.SyncPairs.GroupBy(pair => pair.Provider.Name, StringComparer.Ordinal).Select(group => new ProviderFilterViewModel(group.First().Provider, group.Count(), SelectProvider, Localization))];
        ApplyFilter(_ => true, _ => false);
    }

    private void RebuildAccountCards()
    {
        selectedAccountId = selectedAccountId.Match(id => Overview.Accounts.Any(account => account.Id == id) ? Option.Some(id) : Option.None<AccountId>(), () => Option.None<AccountId>());
        allAccountCards = [.. Overview.Accounts.Select(account => new LayoutCAccountCardViewModel(account, SelectAccount, syncActions, Localization) { IsSelected = selectedAccountId.Match(id => id == account.Id, () => false) })];
        ShowSearchedAccountCards();
        OnPropertyChanged(nameof(SyncPairs));
        OnPropertyChanged(nameof(IsAllPairsSelected));
    }

    private void ShowSearchedAccountCards()
    {
        AccountCards = [.. allAccountCards.Where(card => IsAccountShownBySearch(card.Account))];
        OnPropertyChanged(nameof(AccountCards));
    }

    private bool IsAccountShownBySearch(AccountViewModel account) => account.MatchesSearch(searchText) || Overview.SyncPairs.Any(pair => pair.AccountId == account.Id && pair.MatchesSearch(searchText));

    private bool IsShownBySearch(SyncPairViewModel pair) => pair.MatchesSearch(searchText) || Overview.Accounts.Any(account => account.Id == pair.AccountId && account.MatchesSearch(searchText));

    private void SelectAccount(LayoutCAccountCardViewModel selected)
    {
        selectedAccountId = selected.IsSelected ? Option.None<AccountId>() : Option.Some(selected.Account.Id);

        foreach (var card in allAccountCards)
            card.IsSelected = selectedAccountId.Match(id => id == card.Account.Id, () => false);

        OnPropertyChanged(nameof(SyncPairs));
        OnPropertyChanged(nameof(IsAllPairsSelected));
    }

    private bool IsOfSelectedAccount(SyncPairViewModel pair) => selectedAccountId.Match(id => pair.AccountId == id, () => true);

    private void SelectAllPairs()
    {
        selectedAccountId = Option.None<AccountId>();

        foreach (var card in allAccountCards)
            card.IsSelected = false;

        ApplyFilter(_ => true, _ => false);
    }

    private void SelectProvider(ProviderFilterViewModel selected) => ApplyFilter(selected.Matches, filter => ReferenceEquals(filter, selected));

    private void ApplyFilter(Func<SyncPairViewModel, bool> matches, Func<ProviderFilterViewModel, bool> isSelected)
    {
        foreach (var filter in ProviderFilters)
            filter.IsSelected = isSelected(filter);

        pairFilter = matches;
        OnPropertyChanged(nameof(IsAllPairsSelected));
        OnPropertyChanged(nameof(SyncPairs));
    }

    private async Task AddAccountAndReloadAsync(CancellationToken cancellationToken)
    {
        await syncActions.AddAccountAsync(cancellationToken);
        await Overview.LoadAsync(cancellationToken);
    }
}
