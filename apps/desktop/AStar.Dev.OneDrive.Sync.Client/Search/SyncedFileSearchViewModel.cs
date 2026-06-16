using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Search;

public sealed partial class SyncedFileSearchViewModel(ISyncedItemRepository repository, IFileOpenerService fileOpenerService, IFileTypeClassifier fileTypeClassifier, IAccountRepository accountRepository, IUiDispatcher dispatcher, ILocalizationService loc) : ObservableObject
{
    private readonly IAccountRepository accountRepository = accountRepository;
    private AccountId? activeAccountId;

    [ObservableProperty]
    public partial string? NameFragment { get; set; }

    [ObservableProperty]
    public partial long? MinSize { get; set; }

    [ObservableProperty]
    public partial long? MaxSize { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDuplicateDisclaimer))]
    public partial bool DuplicatesOnly { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial int ResultCount { get; private set; }

    [ObservableProperty]
    public partial int SelectedSortOrderIndex { get; set; }

    public ObservableCollection<SyncedFileResultViewModel> Results { get; } = [];
    public ObservableCollection<string> SelectedTags { get; } = [];
    public ObservableCollection<string> AvailableTags { get; } = [];

    public IReadOnlyList<string> AvailableSortOrders { get; } =
    [
        loc.GetLocal("Search.SortOrder.NameAsc"),
        loc.GetLocal("Search.SortOrder.NameDesc"),
        loc.GetLocal("Search.SortOrder.SizeAsc"),
        loc.GetLocal("Search.SortOrder.SizeDesc")
    ];

    public bool ShowDuplicateDisclaimer => DuplicatesOnly;

    /// <summary>Localised "Search files" heading.</summary>
    public string SearchTitleText => loc.GetLocal("Search.Title");

    /// <summary>Localised "Name" field label.</summary>
    public string NameLabelText => loc.GetLocal("Search.Name.Label");

    /// <summary>Localised placeholder for the name text field.</summary>
    public string NamePlaceholderText => loc.GetLocal("Search.Name.Placeholder");

    /// <summary>Localised "Min size (bytes)" label.</summary>
    public string MinSizeLabelText => loc.GetLocal("Search.MinSize.Label");

    /// <summary>Localised "Max size (bytes)" label.</summary>
    public string MaxSizeLabelText => loc.GetLocal("Search.MaxSize.Label");

    /// <summary>Localised "Tags" label.</summary>
    public string TagsLabelText => loc.GetLocal("Search.Tags.Label");

    /// <summary>Localised message shown when no classifications exist for the active account.</summary>
    public string TagsNoClassificationsText => loc.GetLocal("Search.Tags.NoClassifications");

    /// <summary>Localised "Duplicates only" toggle label.</summary>
    public string DuplicatesOnlyLabelText => loc.GetLocal("Search.DuplicatesOnly.Label");

    /// <summary>Localised "Search" button label.</summary>
    public string SearchButtonText => loc.GetLocal("Search.Button");

    /// <summary>Localised disclaimer shown when DuplicatesOnly is active.</summary>
    public string DuplicateDisclaimerText => loc.GetLocal("Search.DuplicateDisclaimer");

    /// <summary>Localised "No results found." empty-state message.</summary>
    public string NoResultsText => loc.GetLocal("Search.NoResults");

    /// <summary>Localised "Sort by" label for the sort order selector.</summary>
    public string SortOrderLabelText => loc.GetLocal("Search.SortOrder.Label");

    [RelayCommand]
    private void ToggleTag(string tag)
    {
        if (!SelectedTags.Remove(tag))
            SelectedTags.Add(tag);
    }

    public void SetActiveAccount(AccountId accountId)
    {
        activeAccountId = accountId;
        _ = LoadAvailableTagsAsync(accountId);
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        if (activeAccountId is null)
            return;

        IsSearching = true;
        Results.Clear();
        ResultCount = 0;

        try
        {
            var criteria = SyncedItemSearchCriteriaFactory.Create(
                activeAccountId.Value,
                string.IsNullOrWhiteSpace(NameFragment) ? null : NameFragment,
                MinSize,
                MaxSize,
                SelectedTags.Count > 0 ? [.. SelectedTags] : null,
                DuplicatesOnly,
                IndexToSortOrder(SelectedSortOrderIndex));

            var results = await repository.SearchAsync(criteria, cancellationToken);

            dispatcher.Post(() =>
            {
                foreach (var result in results)
                {
                    var vm = new SyncedFileResultViewModel(result, fileTypeClassifier, fileOpenerService, dispatcher);
                    Results.Add(vm);
                    _ = vm.LoadThumbnailAsync();
                }

                ResultCount = Results.Count;
            });
        }
        finally
        {
            IsSearching = false;
        }
    }

    private static SearchSortOrder IndexToSortOrder(int index) => index switch
    {
        1 => SearchSortOrder.NameDescending,
        2 => SearchSortOrder.SizeAscending,
        3 => SearchSortOrder.SizeDescending,
        _ => SearchSortOrder.NameAscending
    };

    private async Task LoadAvailableTagsAsync(AccountId accountId)
    {
        var tags = await repository.GetDistinctTagNamesAsync(accountId, CancellationToken.None).ConfigureAwait(false);

        dispatcher.Post(() =>
        {
            AvailableTags.Clear();
            foreach (var tag in tags)
                AvailableTags.Add(tag);
        });
    }
}
