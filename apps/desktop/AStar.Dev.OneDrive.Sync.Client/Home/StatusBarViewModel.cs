using System.ComponentModel;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

/// <summary>
/// Drives the bottom status bar. Reacts to <see cref="AccountsViewModel.ActiveAccount"/>
/// changes directly, keeping itself in sync without requiring external coordination.
/// </summary>
public sealed partial class StatusBarViewModel : ObservableObject
{
    private readonly AccountsViewModel _accounts;
    private AccountCardViewModel? _trackedCard;

    public StatusBarViewModel(AccountsViewModel accounts)
    {
        _accounts = accounts;
        _accounts.PropertyChanged += OnAccountsPropertyChanged;
        _accounts.ActiveAccountStateChanged += OnActiveAccountStateChanged;
        ApplyActiveAccount(_accounts.ActiveAccount);
    }

    [ObservableProperty]
    public partial string AccountDisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AccountEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasAccount { get; set; }

    [ObservableProperty]
    public partial SyncState SyncState { get; set; } = SyncState.Idle;

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial int ConflictCount { get; set; }

    [ObservableProperty]
    public partial string LastSyncText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSyncing { get; set; }

    [ObservableProperty]
    public partial string StorageUsedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConflictPolicyText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SyncProgress { get; set; }

    /// <summary>Human-readable label for the current sync state.</summary>
    public string StatusLabel => SyncState switch
    {
        SyncState.Syncing  => "Syncing ...",
        SyncState.Pending  => $"{PendingCount} pending",
        SyncState.Conflict => ConflictCount == 1 ? "1 conflict" : $"{ConflictCount} conflicts",
        SyncState.Error    => "Error",
        _                  => "Synced"
    };

    partial void OnSyncStateChanged(SyncState value) => OnPropertyChanged(nameof(StatusLabel));
    partial void OnPendingCountChanged(int value) => OnPropertyChanged(nameof(StatusLabel));
    partial void OnConflictCountChanged(int value) => OnPropertyChanged(nameof(StatusLabel));

    private void OnAccountsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(AccountsViewModel.ActiveAccount))
            ApplyActiveAccount(_accounts.ActiveAccount);
    }

    private void OnActiveAccountStateChanged(object? sender, EventArgs e) => ApplyActiveAccount(_accounts.ActiveAccount);

    private void OnTrackedCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(_trackedCard is null)
            return;

        switch(e.PropertyName)
        {
            case nameof(AccountCardViewModel.SyncState):
                SyncState = _trackedCard.SyncState;
                IsSyncing = _trackedCard.SyncState == SyncState.Syncing;
                break;
            case nameof(AccountCardViewModel.ConflictCount):
                ConflictCount = _trackedCard.ConflictCount;
                break;
            case nameof(AccountCardViewModel.LastSyncText):
                LastSyncText = _trackedCard.LastSyncText;
                break;
        }
    }

    private void ApplyActiveAccount(AccountCardViewModel? active)
    {
        UntrackCurrentCard();

        if(active is null)
        {
            HasAccount = false;
            AccountEmail = string.Empty;
            AccountDisplayName = string.Empty;

            return;
        }

        HasAccount = true;
        AccountEmail = active.Email;
        AccountDisplayName = active.DisplayName;
        SyncState = active.SyncState;
        ConflictCount = active.ConflictCount;
        LastSyncText = active.LastSyncText;
        IsSyncing = active.SyncState == SyncState.Syncing;

        TrackCard(active);
    }

    private void TrackCard(AccountCardViewModel card)
    {
        _trackedCard = card;
        _trackedCard.PropertyChanged += OnTrackedCardPropertyChanged;
    }

    private void UntrackCurrentCard()
    {
        if(_trackedCard is null)
            return;

        _trackedCard.PropertyChanged -= OnTrackedCardPropertyChanged;
        _trackedCard = null;
    }
}
