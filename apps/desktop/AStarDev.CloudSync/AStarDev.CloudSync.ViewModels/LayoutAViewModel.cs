using System.ComponentModel;
using AStarDev.CloudSync.Core;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>Layout A, "unified account list": the shared overview plus the provider filter, account cards and pane text.</summary>
public sealed class LayoutAViewModel : LocalizedObservableObject
{
    private IReadOnlyList<LayoutAAccountCardViewModel> allCards = [];

    /// <summary>Creates the view model and rebuilds it whenever the overview's accounts change.</summary>
    /// <param name="overview">The shared overview.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public LayoutAViewModel(SyncOverviewViewModel overview, ILocalizationService localization) : base(localization)
    {
        Overview = overview;
        overview.PropertyChanged += OnOverviewChanged;
        Rebuild();
    }

    /// <summary>The shared overview: stats, throughput and accounts.</summary>
    public SyncOverviewViewModel Overview { get; }

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

    /// <summary>The accounts pane title.</summary>
    public string AccountsTitle => Localization.GetLocal("LayoutA.Accounts");

    /// <summary>The add account button text.</summary>
    public string AddAccountText => Localization.GetLocal("LayoutA.AddAccount");

    /// <summary>The dashboard rail button name.</summary>
    public string DashboardText => Localization.GetLocal("LayoutA.Nav.Dashboard");

    /// <summary>The folders rail button name.</summary>
    public string FoldersText => Localization.GetLocal("LayoutA.Nav.Folders");

    /// <summary>The activity rail button name.</summary>
    public string ActivityText => Localization.GetLocal("LayoutA.Nav.Activity");

    /// <summary>The conflicts rail button name.</summary>
    public string ConflictsText => Localization.GetLocal("LayoutA.Nav.Conflicts");

    /// <summary>The search rail button name.</summary>
    public string SearchText => Localization.GetLocal("LayoutA.Nav.Search");

    /// <summary>The settings rail button name.</summary>
    public string SettingsText => Localization.GetLocal("LayoutA.Nav.Settings");

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SyncOverviewViewModel.Accounts))
            Rebuild();
    }

    private void Rebuild()
    {
        AccountId? previous = allCards.FirstOrDefault(card => card.IsSelected)?.Account.Id;
        allCards = [.. Overview.Accounts.Select(account => new LayoutAAccountCardViewModel(account, SelectAccount, Localization) { IsSelected = account.Id == previous })];
        var allChip = new LayoutAProviderChipViewModel(() => Localization.GetLocal("LayoutA.Filter.All"), _ => true, Select, Localization);
        var providerChips = Overview.Accounts.Select(account => account.Provider).DistinctBy(provider => provider.Name, StringComparer.Ordinal).Select(provider => new LayoutAProviderChipViewModel(() => provider.Name, account => string.Equals(account.Provider.Name, provider.Name, StringComparison.Ordinal), Select, Localization));
        ProviderChips = [allChip, .. providerChips];

        OnPropertyChanged(nameof(ProviderChips));
        OnPropertyChanged(nameof(AccountCountText));
        OnPropertyChanged(nameof(StatusSummaryText));
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
    }
}
