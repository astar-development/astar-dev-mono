using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using AStar.Dev.Utilities;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Dashboard;

public sealed partial class DashboardAccountViewModel : ObservableObject
{
    private readonly OneDriveAccount _account;
    private readonly SyncScheduler   _scheduler;

    public string AccountId => _account.Id;
    public string DisplayName => _account.DisplayName;
    public string Email => _account.Email;
    public string AccentHex => Accounts.AccountCardViewModel.PaletteHex(_account.AccentIndex);
    public Avalonia.Media.Color AccentColor => Avalonia.Media.Color.Parse(AccentHex);

    public long QuotaTotal => _account.QuotaTotal;
    public long QuotaUsed => _account.QuotaUsed;
    public double StorageFraction => QuotaTotal > 0
        ? Math.Clamp((double)QuotaUsed / QuotaTotal, 0, 1)
        : 0;

    public string StorageText => QuotaTotal > 0
        ? $"{QuotaUsed.FileSizeToText()} / {QuotaTotal.FileSizeToText()}"
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

    public string ExpanderGlyph => IsExpanded ? "\u25BE" : "\u25B8";

    public ObservableCollection<ActivityItemViewModel> RecentActivity { get; } = [];

    public DashboardAccountViewModel(OneDriveAccount account, SyncScheduler scheduler, IAccountRepository repository, ILocalizationService localizationService)
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
    private async Task SyncNowAsync()
    {
        var entity = await _repository.GetByIdAsync(_account.Id, CancellationToken.None);
        if(entity is null)
            return;

        var fullAccount = new OneDriveAccount
        {
            Id                = entity.Id,
            DisplayName       = entity.DisplayName,
            Email             = entity.Email,
            LocalSyncPath     = entity.LocalSyncPath,
            ConflictPolicy    = entity.ConflictPolicy,
            SelectedFolderIds = [.. entity.SyncFolders.Select(f => f.FolderId)],
            LastSyncedAt      = entity.LastSyncedAt
        };
        AddRecentActivity(new ActivityItemViewModel(){FileName = _localizationService.GetLocal("Sync.Starting")});
        await _scheduler.TriggerAccountAsync(fullAccount);
    }

    public void UpdateSyncState(SyncState state, int conflicts)
        => Dispatcher.UIThread.Post(() =>
                                    {
                                        SyncState = state;
                                        ConflictCount = conflicts;
                                        IsSyncing = state == SyncState.Syncing;

                                        if (state != SyncState.Idle) return;

                                        _account.LastSyncedAt = DateTimeOffset.UtcNow;
                                        UpdateLastSyncText(SyncState);
                                    });

    public void AddRecentActivity(ActivityItemViewModel item)
        => Dispatcher.UIThread.Post(() =>
                                    {
                                        RecentActivity.Insert(0, item);
                                        while(RecentActivity.Count > 3)
                                            RecentActivity.RemoveAt(RecentActivity.Count - 1);
                                    });

    private void UpdateLastSyncText(SyncState syncState)
        => LastSyncText =
        syncState == SyncState.NoSyncPathConfigured ? "No local sync path configured" :
         _account.LastSyncedAt is null
            ? "Never synced"
            : (DateTimeOffset.UtcNow - _account.LastSyncedAt.Value) switch
            {
                { TotalSeconds: < 60 } => "Just now",
                { TotalMinutes: < 60 } td => $"{(int)td.TotalMinutes}m ago",
                { TotalHours: < 24 } td => $"{(int)td.TotalHours}h ago",
                { TotalDays: < 2 } => "Yesterday",
                var td => $"{(int)td.TotalDays}d ago"
            };
}
