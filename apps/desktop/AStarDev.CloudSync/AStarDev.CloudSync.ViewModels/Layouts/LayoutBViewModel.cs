using System.ComponentModel;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>Layout B, grouped by provider: a sidebar tree of providers and their accounts, and the selected account's detail.</summary>
public sealed class LayoutBViewModel : LocalizedObservableObject
{
    private readonly LocalPathFormatter localPathFormatter;
    private readonly ISyncActions syncActions;
    private LayoutBDetailViewModel detail;
    private string searchText = string.Empty;
    private AccountId shownAccountId;

    /// <summary>Creates the layout over the shared overview and rebuilds it whenever the overview's accounts or sync pairs change.</summary>
    /// <param name="overview">The shared overview every layout binds to.</param>
    /// <param name="localPathFormatter">Shortens the synced folders' local paths.</param>
    /// <param name="syncActions">Carries out the account actions the user picks.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public LayoutBViewModel(SyncOverviewViewModel overview, LocalPathFormatter localPathFormatter, ISyncActions syncActions, ILocalizationService localization) : base(localization)
    {
        Overview = overview;
        this.localPathFormatter = localPathFormatter;
        this.syncActions = syncActions;
        ConnectProviderCommand = new AsyncRelayCommand(AddAccountAndReloadAsync);
        detail = new NoAccountDetailViewModel(localization);
        overview.PropertyChanged += OnOverviewChanged;
        Rebuild();
    }

    /// <summary>The shared overview.</summary>
    public SyncOverviewViewModel Overview { get; }

    /// <summary>The accounts grouped by provider, in overview order.</summary>
    public IReadOnlyList<ProviderGroupViewModel> ProviderGroups { get; private set; } = [];

    /// <summary>The detail pane content: the selected account, or a no-accounts message.</summary>
    public LayoutBDetailViewModel Detail
    {
        get => detail;
        private set
        {
            if (SetProperty(ref detail, value))
                OnPropertyChanged(nameof(SelectedItem));
        }
    }

    /// <summary>The tree's selected node. Setting an account selects it; any other node keeps the current selection.</summary>
    public object SelectedItem
    {
        get => detail is AccountDetailViewModel accountDetail ? accountDetail.Account : detail;
        set
        {
            if (value is AccountViewModel account)
                Select(account);
            else
                OnPropertyChanged();
        }
    }

    /// <summary>How many accounts are syncing, e.g. "1 account syncing".</summary>
    public string SyncingSummaryText
    {
        get
        {
            int syncing = Overview.Accounts.Count(account => account.Status.IsSyncing);

            return Localization.GetLocal(syncing == 1 ? "LayoutB.AccountsSyncing.One" : "LayoutB.AccountsSyncing.Many", syncing.ToString("N0", Localization.CurrentCulture));
        }
    }

    /// <summary>The sidebar search text. Accounts and folders whose name, email, provider or path contain it, ignoring case and surrounding spaces, are shown; blank shows everything.</summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value ?? string.Empty))
                Rebuild();
        }
    }

    /// <summary>The search box placeholder.</summary>
    public string SearchWatermark => Localization.GetLocal("LayoutB.Search");

    /// <summary>The connect-a-provider action text.</summary>
    public string ConnectProviderText => Localization.GetLocal("LayoutB.ConnectProvider");

    /// <summary>Starts connecting a new account, from any provider.</summary>
    public IAsyncRelayCommand ConnectProviderCommand { get; }

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(SyncOverviewViewModel.Accounts) or nameof(SyncOverviewViewModel.SyncPairs))
            Rebuild();
    }

    private void Rebuild()
    {
        List<AccountViewModel> visibleAccounts = [.. Overview.Accounts.Where(IsVisible)];
        ProviderGroups = [.. visibleAccounts.GroupBy(account => account.Provider.Name, StringComparer.Ordinal).Select(group => new ProviderGroupViewModel(group.First().Provider, [.. group], Localization))];
        OnPropertyChanged(nameof(ProviderGroups));
        OnPropertyChanged(nameof(SyncingSummaryText));

        if (Overview.Accounts.Count == 0)
        {
            Detail = new NoAccountDetailViewModel(Localization);

            return;
        }

        if (visibleAccounts.Count == 0)
        {
            Detail = new NoMatchingAccountsDetailViewModel(Localization);

            return;
        }

        Select(visibleAccounts.FirstOrDefault(account => account.Id == shownAccountId) ?? visibleAccounts[0]);
    }

    private void Select(AccountViewModel account)
    {
        shownAccountId = account.Id;
        Detail = new AccountDetailViewModel(account, [.. Overview.SyncPairs.Where(pair => pair.AccountId == account.Id && (AccountMatches(account) || PairMatches(pair)))], Overview.Activity, localPathFormatter, syncActions, Localization);
    }

    private bool IsVisible(AccountViewModel account) => AccountMatches(account) || Overview.SyncPairs.Any(pair => pair.AccountId == account.Id && PairMatches(pair));

    private bool AccountMatches(AccountViewModel account) => Matches(account.Name) || Matches(account.Email) || Matches(account.Provider.Name);

    private bool PairMatches(SyncPairViewModel pair) => Matches(pair.Name) || Matches(pair.LocalPath) || Matches(pair.RemotePath);

    private bool Matches(string text) => text.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase);

    private async Task AddAccountAndReloadAsync(CancellationToken cancellationToken)
    {
        await syncActions.AddAccountAsync(cancellationToken);
        await Overview.LoadAsync(cancellationToken);
    }
}
