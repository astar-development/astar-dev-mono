using System.Collections.ObjectModel;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Dashboard;

public sealed partial class DashboardAccountViewModel : ObservableObject
{
    private readonly OneDriveAccount _account;
    private readonly ISyncScheduler _scheduler;

    /// <summary>Raw string account ID — unwrapped at the display boundary.</summary>
    public string AccountId => _account.Id.Id;
    public string DisplayName => _account.Profile.DisplayName;
    public string Email => _account.Profile.Email;
    public string AccentHex => Accounts.AccountCardViewModel.PaletteHex(_account.AccentIndex);
    public Avalonia.Media.Color AccentColor => Avalonia.Media.Color.Parse(AccentHex);

    public long QuotaTotal => _account.Quota.TotalBytes;
    public long QuotaUsed => _account.Quota.UsedBytes;
    public double StorageFraction => _account.Quota.Fraction();
    public string StorageText => _account.Quota.TotalBytes > 0
        ? $"{_account.Quota.UsedBytes.FileSizeToText()} / {_account.Quota.TotalBytes.FileSizeToText()}"
        : "Unknown";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(IsHealthy))]
    public partial SyncState SyncState { get; set; } = SyncState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(IsHealthy))]
    public partial int ConflictCount { get; set; }

    [ObservableProperty]
    public partial string LastSyncText { get; set; } = "Never synced";

    [ObservableProperty]
    public partial int FolderCount { get; set; }

    private readonly IAccountRepository _repository;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    public partial bool IsSyncing { get; set; }

    public bool IsHealthy => SyncState is SyncState.Idle && ConflictCount == 0;
    public string StatusLabel => (SyncState, ConflictCount) switch
    {
        (SyncState.Syncing, _) => "Syncing ...",
        (SyncState.Error, _) => "Error",
        (_, > 0) => $"{ConflictCount} conflict{(ConflictCount == 1 ? "" : "s")}",
        (SyncState.Pending, _) => "Pending",
        _ => "Synced"
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpanderGlyph))]
    public partial bool IsExpanded { get; set; } = true;

    public string ExpanderGlyph => IsExpanded ? "▾" : "▸";

    public ObservableCollection<ActivityItemViewModel> RecentActivity { get; } = [];

    public DashboardAccountViewModel(OneDriveAccount account, ISyncScheduler scheduler, IAccountRepository repository, ILocalizationService localizationService)
    {
        _account = account;
        _scheduler = scheduler;
        FolderCount = account.SelectedFolderIds.Count;
        _repository = repository;
        _localizationService = localizationService;
        UpdateLastSyncText(SyncState.Idle);
    }

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private Task CancelSyncAsync() => _scheduler.CancelAccountSyncAsync(_account.Id.Id);

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        AddRecentActivity(new ActivityItemViewModel { FileName = _localizationService.GetLocal("Sync.Starting") });

        await _repository.GetByIdAsync(_account.Id, CancellationToken.None)
            .TapAsync(async entity =>
            {
                var fullAccount = new OneDriveAccount
                {
                    Id = entity.Id,
                    Profile = entity.Profile,
                    SyncConfig = entity.SyncConfig.LocalSyncPath.Value.Length > 0 ? Option.Some(entity.SyncConfig) : Option.None<AccountSyncConfig>(),
                    LastSyncedAt = entity.LastSyncedAt
                };
                await _scheduler.TriggerAccountAsync(fullAccount);
            });
    }

    public void UpdateSyncState(SyncState state, int conflicts)
    {
        SyncState = state;
        ConflictCount = conflicts;
        IsSyncing = state == SyncState.Syncing;

        if (state is not (SyncState.Idle or SyncState.Completed)) return;

        _account.LastSyncedAt = Option.Some(DateTimeOffset.UtcNow);
        UpdateLastSyncText(state);
    }

    public void AddRecentActivity(ActivityItemViewModel item)
    {
        RecentActivity.Insert(0, item);
        while (RecentActivity.Count > 3)
            RecentActivity.RemoveAt(RecentActivity.Count - 1);
    }

    private void UpdateLastSyncText(SyncState syncState)
        => LastSyncText =
        syncState == SyncState.NoSyncPathConfigured ? "No local sync path configured" :
        _account.LastSyncedAt is null ? "Never synced" :
        _account.LastSyncedAt.Match(
            lastSyncedAt => (DateTimeOffset.UtcNow - lastSyncedAt) switch
            {
                { TotalSeconds: < 60 } => "Just now",
                { TotalMinutes: < 60 } td => $"{(int)td.TotalMinutes}m ago",
                { TotalHours: < 24 } td => $"{(int)td.TotalHours}h ago",
                { TotalDays: < 2 } => "Yesterday",
                var td => $"{(int)td.TotalDays}d ago"
            },
            () => "Never synced");
}
