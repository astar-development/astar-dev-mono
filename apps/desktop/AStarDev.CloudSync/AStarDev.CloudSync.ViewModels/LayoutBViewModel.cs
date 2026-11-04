using System.ComponentModel;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>Layout B, grouped by provider: a sidebar tree of providers and their accounts, and the selected account's detail.</summary>
public sealed class LayoutBViewModel : LocalizedObservableObject
{
    private LayoutBDetailViewModel detail;

    /// <summary>Creates the layout over the shared overview and rebuilds it whenever the overview's accounts or sync pairs change.</summary>
    /// <param name="overview">The shared overview every layout binds to.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public LayoutBViewModel(SyncOverviewViewModel overview, ILocalizationService localization) : base(localization)
    {
        Overview = overview;
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

    /// <summary>The search box placeholder.</summary>
    public string SearchWatermark => Localization.GetLocal("LayoutB.Search");

    /// <summary>The connect-a-provider action text.</summary>
    public string ConnectProviderText => Localization.GetLocal("LayoutB.ConnectProvider");

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(SyncOverviewViewModel.Accounts) or nameof(SyncOverviewViewModel.SyncPairs))
            Rebuild();
    }

    private void Rebuild()
    {
        ProviderGroups = [.. Overview.Accounts.GroupBy(account => account.Provider.Name, StringComparer.Ordinal).Select(group => new ProviderGroupViewModel(group.First().Provider, [.. group], Localization))];
        OnPropertyChanged(nameof(ProviderGroups));
        OnPropertyChanged(nameof(SyncingSummaryText));

        if (Overview.Accounts.Count == 0)
        {
            Detail = new NoAccountDetailViewModel(Localization);

            return;
        }

        var previous = detail is AccountDetailViewModel accountDetail ? accountDetail.Account.Id : default;
        Select(Overview.Accounts.FirstOrDefault(account => account.Id == previous) ?? Overview.Accounts[0]);
    }

    private void Select(AccountViewModel account) => Detail = new AccountDetailViewModel(account, [.. Overview.SyncPairs.Where(pair => pair.AccountId == account.Id)], Localization);
}
