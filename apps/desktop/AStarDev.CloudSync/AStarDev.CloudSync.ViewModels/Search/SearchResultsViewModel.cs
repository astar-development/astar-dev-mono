using System.ComponentModel;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels.Search;

/// <summary>The search page: what the user typed and the accounts and folders it matches. A blank search lists everything.</summary>
public sealed class SearchResultsViewModel : LocalizedObservableObject
{
    private readonly SyncOverviewViewModel overview;
    private string searchText = string.Empty;

    /// <summary>Creates the page and follows the overview's accounts and sync pairs.</summary>
    /// <param name="overview">The shared overview whose accounts and sync pairs are searched.</param>
    /// <param name="localization">Supplies the text and culture.</param>
    public SearchResultsViewModel(SyncOverviewViewModel overview, ILocalizationService localization) : base(localization)
    {
        this.overview = overview;
        overview.PropertyChanged += OnOverviewChanged;
        Refresh();
    }

    /// <summary>What the user typed. Accounts and folders whose name, email, provider or path contain it, ignoring case and surrounding spaces, are listed.</summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value ?? string.Empty))
                Refresh();
        }
    }

    /// <summary>The accounts matching the search.</summary>
    public IReadOnlyList<AccountViewModel> Accounts { get; private set; } = [];

    /// <summary>The folders (sync pairs) matching the search.</summary>
    public IReadOnlyList<SyncPairViewModel> Folders { get; private set; } = [];

    /// <summary>Whether nothing matches.</summary>
    public bool IsEmpty => Accounts.Count == 0 && Folders.Count == 0;

    /// <summary>The search box placeholder.</summary>
    public string PlaceholderText => Localization.GetLocal("Search.Placeholder");

    /// <summary>The heading above the matching accounts.</summary>
    public string AccountsHeadingText => Localization.GetLocal("Search.Accounts");

    /// <summary>The heading above the matching folders.</summary>
    public string FoldersHeadingText => Localization.GetLocal("Search.Folders");

    /// <summary>Shown when nothing matches.</summary>
    public string NoResultsText => Localization.GetLocal("Search.NoResults");

    private void OnOverviewChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(SyncOverviewViewModel.Accounts) or nameof(SyncOverviewViewModel.SyncPairs))
            Refresh();
    }

    private void Refresh()
    {
        Accounts = [.. overview.Accounts.Where(account => account.MatchesSearch(searchText))];
        Folders = [.. overview.SyncPairs.Where(pair => pair.MatchesSearch(searchText))];
        OnPropertyChanged(nameof(Accounts));
        OnPropertyChanged(nameof(Folders));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
