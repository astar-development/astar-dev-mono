using System.ComponentModel;
using System.Windows.Input;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.CloudSync.ViewModels.Classification;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.Search;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>Layout A, "unified account list": the shared overview plus the provider filter, account cards and pane text.</summary>
public sealed class LayoutAViewModel : LocalizedObservableObject
{
    private readonly ISyncActions syncActions;
    private IReadOnlyList<LayoutAAccountCardViewModel> allCards = [];
    private LayoutAPage page = LayoutAPageFactory.CreateAccounts();

    /// <summary>Creates the view model and rebuilds it whenever the overview's accounts change.</summary>
    /// <param name="overview">The shared overview.</param>
    /// <param name="syncActions">Carries out the account actions the user picks.</param>
    /// <param name="conflictLists">Creates the conflict list of the Conflicts page.</param>
    /// <param name="folderSelections">Creates the folder selection of the Folders page.</param>
    /// <param name="activityLists">Creates the activity list of the Activity page.</param>
    /// <param name="classification">The Classification page.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public LayoutAViewModel(SyncOverviewViewModel overview, ISyncActions syncActions, IConflictListFactory conflictLists, IFolderSelectionFactory folderSelections, IActivityListFactory activityLists, ClassificationPageViewModel classification, ILocalizationService localization) : base(localization)
    {
        Overview = overview;
        ConflictList = conflictLists.Create(Option.None<AccountId>());
        Folders = folderSelections.CreatePage();
        Activity = activityLists.Create(overview, Option.None<AccountId>());
        Classification = classification;
        Search = new SearchResultsViewModel(overview, OpenAccount, OpenFolder, localization);
        this.syncActions = syncActions;
        AddAccountCommand = new AsyncRelayCommand(AddAccountAndReloadAsync);
        overview.PropertyChanged += OnOverviewChanged;
        Rebuild();
    }

    /// <summary>The shared overview: stats, throughput and accounts.</summary>
    public SyncOverviewViewModel Overview { get; }

    /// <summary>The conflicts of every account, shown on the Conflicts page and read each time that page is chosen.</summary>
    public ConflictListViewModel ConflictList { get; }

    /// <summary>The folder selection of the selected account, shown on the Folders page and pointed at the selected account while that page is shown.</summary>
    public FolderSelectionViewModel Folders { get; }

    /// <summary>The activity of every account, shown on the Activity page and read each time that page is chosen.</summary>
    public ActivityListViewModel Activity { get; }

    /// <summary>The classification page, shown on the Classification page and read each time that page is chosen.</summary>
    public ClassificationPageViewModel Classification { get; }

    /// <summary>The search page: what was typed and the accounts and folders it matches.</summary>
    public SearchResultsViewModel Search { get; }

    /// <summary>Whether the search page is shown.</summary>
    public bool IsSearchPageShown => page is LayoutAPage.Search;

    /// <summary>Whether the activity page is shown.</summary>
    public bool IsActivityPageShown => page is LayoutAPage.Activity;

    /// <summary>Whether the folders page is shown.</summary>
    public bool IsFoldersPageShown => page is LayoutAPage.Folders;

    /// <summary>Whether the account cards and stats are shown.</summary>
    public bool IsAccountsPageShown => page is LayoutAPage.Accounts;

    /// <summary>Whether the conflicts page is shown.</summary>
    public bool IsConflictsPageShown => page is LayoutAPage.Conflicts;

    /// <summary>Whether the classification page is shown.</summary>
    public bool IsClassificationPageShown => page is LayoutAPage.Classification;

    /// <summary>"All" followed by one chip per provider present in the accounts.</summary>
    public IReadOnlyList<LayoutAProviderChipViewModel> ProviderChips { get; private set; } = [];

    /// <summary>The accounts passing the selected chip's filter. One is selected while any are visible: the first by default, and the user's choice while it stays visible.</summary>
    public IReadOnlyList<LayoutAAccountCardViewModel> VisibleAccounts { get; private set; } = [];

    /// <summary>The number of accounts, formatted for the culture.</summary>
    public string AccountCountText => Overview.Accounts.Count.ToString("N0", Localization.CurrentCulture);

    /// <summary>The status bar summary, e.g. "Syncing 1 of 4 accounts".</summary>
    public string StatusSummaryText
    {
        get
        {
            int syncing = Overview.Accounts.Count(account => account.Status.IsSyncing);

            if (syncing == 0)
                return Localization.GetLocal("LayoutA.StatusBar.Idle");

            string key = Overview.Accounts.Count == 1 ? "LayoutA.StatusBar.Syncing.One" : "LayoutA.StatusBar.Syncing.Many";

            return Localization.GetLocal(key, syncing.ToString("N0", Localization.CurrentCulture), Overview.Accounts.Count.ToString("N0", Localization.CurrentCulture));
        }
    }

    /// <summary>The status bar check summary, e.g. "Up to date · checked 10 min ago".</summary>
    public string StatusCheckedText => Overview.LastChecked.Match(_ => Localization.GetLocal("LayoutA.StatusBar.Checked", Overview.OverallStatusText(), Overview.LastCheckedText), () => Localization.GetLocal("LayoutA.StatusBar.NeverChecked", Overview.OverallStatusText()));

    /// <summary>The tooltip of the status bar check summary: it covers every account, not one.</summary>
    public string StatusCheckedTooltip => Localization.GetLocal("LayoutA.StatusBar.CheckedTooltip");

    /// <summary>The accounts pane title.</summary>
    public string AccountsTitle => Localization.GetLocal("LayoutA.Accounts");

    /// <summary>The add account button text.</summary>
    public string AddAccountText => Localization.GetLocal("LayoutA.AddAccount");

    /// <summary>Starts connecting a new account.</summary>
    public IAsyncRelayCommand AddAccountCommand { get; }

    /// <summary>Shows the accounts page.</summary>
    public ICommand ShowAccountsCommand => field ??= new RelayCommand(() => Show(LayoutAPageFactory.CreateAccounts()));

    /// <summary>Shows the activity page and reads the activity again.</summary>
    public ICommand ShowActivityCommand => field ??= new RelayCommand(ShowActivity);

    /// <summary>Shows the folders page for the selected account.</summary>
    public ICommand ShowFoldersCommand => field ??= new RelayCommand(ShowFolders);

    /// <summary>Shows the conflicts page and reads the conflicts again.</summary>
    public ICommand ShowConflictsCommand => field ??= new RelayCommand(ShowConflicts);

    /// <summary>Shows the classification page and reads the term list again.</summary>
    public ICommand ShowClassificationCommand => field ??= new RelayCommand(ShowClassification);

    /// <summary>Shows the search page.</summary>
    public ICommand ShowSearchCommand => field ??= new RelayCommand(() => Show(LayoutAPageFactory.CreateSearch()));

    /// <summary>The dashboard rail button name.</summary>
    public string DashboardText => Localization.GetLocal("LayoutA.Nav.Dashboard");

    /// <summary>The folders rail button name.</summary>
    public string FoldersText => Localization.GetLocal("LayoutA.Nav.Folders");

    /// <summary>The activity rail button name.</summary>
    public string ActivityText => Localization.GetLocal("LayoutA.Nav.Activity");

    /// <summary>The conflicts rail button name.</summary>
    public string ConflictsText => Localization.GetLocal("LayoutA.Nav.Conflicts");

    /// <summary>The classification rail button name.</summary>
    public string ClassificationText => Localization.GetLocal("LayoutA.Nav.Classification");

    /// <summary>The search rail button name.</summary>
    public string SearchText => Localization.GetLocal("LayoutA.Nav.Search");

    private void OpenAccount(AccountViewModel account)
    {
        SelectOnly(account.Id);
        Show(LayoutAPageFactory.CreateAccounts());
    }

    private void OpenFolder(SyncPairViewModel pair)
    {
        SelectOnly(pair.AccountId);
        ShowFolders();
    }

    private void SelectOnly(AccountId accountId)
    {
        Select(ProviderChips[0]);
        SelectAccount(allCards.FirstOrDefault(card => card.Account.Id == accountId));
    }

    private void ShowActivity()
    {
        Show(LayoutAPageFactory.CreateActivity());
        _ = Activity.RefreshCommand.ExecuteAsync(null);
    }

    private void ShowFolders()
    {
        Show(LayoutAPageFactory.CreateFolders());
        ShowSelectedAccountFolders();
    }

    private void ShowSelectedAccountFolders()
    {
        var selected = allCards.FirstOrDefault(card => card.IsSelected);

        if (selected is not null)
            _ = Folders.ShowAccountCommand.ExecuteAsync(selected.Account.Id);
    }

    private void ShowConflicts()
    {
        Show(LayoutAPageFactory.CreateConflicts());
        _ = ConflictList.RefreshCommand.ExecuteAsync(null);
    }

    private void ShowClassification()
    {
        Show(LayoutAPageFactory.CreateClassification());
        _ = Classification.LoadCommand.ExecuteAsync(null);
    }

    private void Show(LayoutAPage shown)
    {
        page = shown;
        OnPropertyChanged(nameof(IsSearchPageShown));
        OnPropertyChanged(nameof(IsActivityPageShown));
        OnPropertyChanged(nameof(IsFoldersPageShown));
        OnPropertyChanged(nameof(IsAccountsPageShown));
        OnPropertyChanged(nameof(IsConflictsPageShown));
        OnPropertyChanged(nameof(IsClassificationPageShown));
    }

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SyncOverviewViewModel.Accounts))
            Rebuild();
    }

    private void Rebuild()
    {
        AccountId? previous = allCards.FirstOrDefault(card => card.IsSelected)?.Account.Id;
        allCards = [.. Overview.Accounts.Select(account => new LayoutAAccountCardViewModel(account, SelectAccount, syncActions, Localization) { IsSelected = account.Id == previous })];
        var allChip = new LayoutAProviderChipViewModel(() => Localization.GetLocal("LayoutA.Filter.All"), _ => true, Select, Localization);
        var providerChips = Overview.Accounts.Select(account => account.Provider).DistinctBy(provider => provider.Name, StringComparer.Ordinal).Select(provider => new LayoutAProviderChipViewModel(() => provider.Name, account => string.Equals(account.Provider.Name, provider.Name, StringComparison.Ordinal), Select, Localization));
        ProviderChips = [allChip, .. providerChips];

        OnPropertyChanged(nameof(ProviderChips));
        OnPropertyChanged(nameof(AccountCountText));
        OnPropertyChanged(nameof(StatusSummaryText));
        OnPropertyChanged(nameof(StatusCheckedText));
        Select(allChip);
    }

    private void Select(LayoutAProviderChipViewModel chip)
    {
        foreach (var candidate in ProviderChips)
            candidate.IsSelected = ReferenceEquals(candidate, chip);

        VisibleAccounts = [.. allCards.Where(card => chip.Matches(card.Account))];
        OnPropertyChanged(nameof(VisibleAccounts));
        SelectAccount(VisibleAccounts.OrderByDescending(card => card.IsSelected).FirstOrDefault());
    }

    private void SelectAccount(LayoutAAccountCardViewModel? selected)
    {
        foreach (var card in allCards)
            card.IsSelected = ReferenceEquals(card, selected);

        if (page is LayoutAPage.Folders)
            ShowSelectedAccountFolders();
    }

    private async Task AddAccountAndReloadAsync(CancellationToken cancellationToken)
    {
        await syncActions.AddAccountAsync(cancellationToken);
        await Overview.LoadAsync(cancellationToken);
    }
}
