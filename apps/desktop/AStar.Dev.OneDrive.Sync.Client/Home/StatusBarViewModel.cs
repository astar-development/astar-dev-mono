using System.ComponentModel;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

/// <summary>
/// Drives the bottom status bar. Reacts to <see cref="AccountsViewModel.ActiveAccount"/>
/// changes directly, keeping itself in sync without requiring external coordination.
/// </summary>
public sealed partial class StatusBarViewModel : ObservableObject
{
    private readonly AccountsViewModel accounts;
    private readonly ILocalizationService loc;
    private AccountCardViewModel? trackedCard;

    public StatusBarViewModel(AccountsViewModel accounts, ILocalizationService localizationService)
    {
        this.accounts = accounts;
        accounts.PropertyChanged += OnAccountsPropertyChanged;
        accounts.ActiveAccountStateChanged += OnActiveAccountStateChanged;
        ApplyActiveAccount(accounts.ActiveAccount);
        loc = localizationService;
        loc.CultureChanged += OnCultureChanged;
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
        SyncState.Syncing  => loc.GetLocal("StatusBar.Syncing"),
        SyncState.Pending  => loc.GetLocal("StatusBar.Pending", PendingCount),
        SyncState.Conflict => ConflictCount == 1 ? loc.GetLocal("StatusBar.Conflict", ConflictCount) : loc.GetLocal("StatusBar.Conflicts", ConflictCount),
        SyncState.Error    => loc.GetLocal("StatusBar.Error"),
        _                  => loc.GetLocal("StatusBar.Synced")
    };

    /// <summary>Localised "No account selected" placeholder shown when no account is active.</summary>
    public string NoAccountSelectedText => loc.GetLocal("MainWindow.NoAccountSelected");

    partial void OnSyncStateChanged(SyncState value) => OnPropertyChanged(nameof(StatusLabel));
    partial void OnPendingCountChanged(int value) => OnPropertyChanged(nameof(StatusLabel));
    partial void OnConflictCountChanged(int value) => OnPropertyChanged(nameof(StatusLabel));

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo culture)
    {
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(NoAccountSelectedText));
    }

    private void OnAccountsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(AccountsViewModel.ActiveAccount))
            ApplyActiveAccount(accounts.ActiveAccount);
    }

    private void OnActiveAccountStateChanged(object? sender, EventArgs e) => ApplyActiveAccount(accounts.ActiveAccount);

    private void OnTrackedCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(trackedCard is null)
            return;

        switch(e.PropertyName)
        {
            case nameof(AccountCardViewModel.SyncState):
                SyncState = trackedCard.SyncState;
                IsSyncing = trackedCard.SyncState == SyncState.Syncing;
                break;
            case nameof(AccountCardViewModel.ConflictCount):
                ConflictCount = trackedCard.ConflictCount;
                break;
            case nameof(AccountCardViewModel.LastSyncText):
                LastSyncText = trackedCard.LastSyncText;
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
        trackedCard = card;
        trackedCard.PropertyChanged += OnTrackedCardPropertyChanged;
    }

    private void UntrackCurrentCard()
    {
        if(trackedCard is null)
            return;

        trackedCard.PropertyChanged -= OnTrackedCardPropertyChanged;
        trackedCard = null;
    }
}
