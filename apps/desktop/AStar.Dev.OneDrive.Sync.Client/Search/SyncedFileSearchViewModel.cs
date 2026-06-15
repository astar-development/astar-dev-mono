using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Search;

public sealed partial class SyncedFileSearchViewModel(ISyncedItemRepository repository, IFileOpenerService fileOpenerService, IFileTypeClassifier fileTypeClassifier, IAccountRepository accountRepository, IUiDispatcher dispatcher) : ObservableObject
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

    public ObservableCollection<SyncedFileResultViewModel> Results { get; } = [];
    public ObservableCollection<string> SelectedTags { get; } = [];
    public ObservableCollection<string> AvailableTags { get; } = [];

    public bool ShowDuplicateDisclaimer => DuplicatesOnly;

    public void SetActiveAccount(AccountId accountId) => activeAccountId = accountId;

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
                DuplicatesOnly);

            var results = await repository.SearchAsync(criteria, cancellationToken);

            dispatcher.Post(() =>
            {
                foreach (var result in results)
                    Results.Add(new SyncedFileResultViewModel(result, fileTypeClassifier, fileOpenerService, dispatcher));

                ResultCount = Results.Count;
            });
        }
        finally
        {
            IsSearching = false;
        }
    }
}
