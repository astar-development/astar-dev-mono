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
        : _localizationService.GetLocal("Common.Unknown");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(IsHealthy))]
    public partial SyncState SyncState { get; set; } = SyncState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(IsHealthy))]
    public partial int ConflictCount { get; set; }

    [ObservableProperty]
    public partial string LastSyncText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int FolderCount { get; set; }

    private readonly IAccountRepository _repository;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    public partial bool IsSyncing { get; set; }

    public bool IsHealthy => SyncState is SyncState.Idle && ConflictCount == 0;
    public bool HasEverSynced => _account.LastSyncedAt?.Match(_ => true, () => false) ?? false;
    public string StatusLabel => (SyncState, ConflictCount) switch
    {
        (SyncState.Syncing, _) => _localizationService.GetLocal("StatusBar.Syncing"),
        (SyncState.Error, _) => _localizationService.GetLocal("StatusBar.Error"),
        (_, > 0) => ConflictCount == 1
            ? _localizationService.GetLocal("StatusBar.Conflict", ConflictCount)
            : _localizationService.GetLocal("StatusBar.Conflicts", ConflictCount),
        (SyncState.Pending, _) => _localizationService.GetLocal("Dashboard.Pending"),
        _ => _localizationService.GetLocal("StatusBar.Synced")
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpanderGlyph))]
    public partial bool IsExpanded { get; set; } = true;

    public string ExpanderGlyph => IsExpanded ? "▾" : "▸";

    public ObservableCollection<ActivityItemViewModel> RecentActivity { get; } = [];

    /// <summary>Localised "Storage" section heading.</summary>
    public string StorageHeadingText => _localizationService.GetLocal("Dashboard.Storage");

    /// <summary>Localised "folders" stat label.</summary>
    public string FoldersLabelText => _localizationService.GetLocal("Dashboard.Folders");

    /// <summary>Localised "conflicts" stat label.</summary>
    public string ConflictsLabelText => _localizationService.GetLocal("Dashboard.Conflicts");

    /// <summary>Localised "last sync" stat label.</summary>
    public string LastSyncLabelText => _localizationService.GetLocal("Dashboard.LastSync");

    /// <summary>Localised "Recent activity" section heading.</summary>
    public string RecentActivityText => _localizationService.GetLocal("Dashboard.RecentActivity");

    /// <summary>Localised "No recent activity" empty state.</summary>
    public string NoRecentActivityText => _localizationService.GetLocal("Dashboard.NoRecentActivity");

    /// <summary>Localised "Sync now" button label.</summary>
    public string SyncNowButtonText => _localizationService.GetLocal("Dashboard.SyncNow");

    /// <summary>Localised "Cancel" button label.</summary>
    public string CancelButtonText => _localizationService.GetLocal("Common.Cancel");

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
        AddRecentActivity(new ActivityItemViewModel(_localizationService) { FileName = _localizationService.GetLocal("Sync.Starting") });

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

        if (state is not (SyncState.Idle or SyncState.Completed or SyncState.NoSyncPathConfigured)) return;

        if (state is not SyncState.NoSyncPathConfigured)
            _account.LastSyncedAt = Option.Some(DateTimeOffset.UtcNow);

        UpdateLastSyncText(state);
    }

    /// <summary>Updates the displayed storage quota. Call after a successful quota refresh from the Graph API.</summary>
    public void UpdateQuota(StorageQuota quota)
    {
        _account.Quota = quota;
        OnPropertyChanged(nameof(QuotaTotal));
        OnPropertyChanged(nameof(QuotaUsed));
        OnPropertyChanged(nameof(StorageFraction));
        OnPropertyChanged(nameof(StorageText));
    }

    public void AddRecentActivity(ActivityItemViewModel item)
    {
        RecentActivity.Insert(0, item);
        while (RecentActivity.Count > 3)
            RecentActivity.RemoveAt(RecentActivity.Count - 1);
    }

    private void UpdateLastSyncText(SyncState syncState)
        => LastSyncText =
        syncState == SyncState.NoSyncPathConfigured ? _localizationService.GetLocal("Dashboard.NoSyncPath") :
        _account.LastSyncedAt is null ? _localizationService.GetLocal("Common.NeverSynced") :
        _account.LastSyncedAt.Match(
            lastSyncedAt => (DateTimeOffset.UtcNow - lastSyncedAt) switch
            {
                { TotalSeconds: < 60 } => _localizationService.GetLocal("Common.JustNow"),
                { TotalMinutes: < 60 } td => _localizationService.GetLocal("Common.MinutesAgo", (int)td.TotalMinutes),
                { TotalHours: < 24 } td => _localizationService.GetLocal("Common.HoursAgo", (int)td.TotalHours),
                { TotalDays: < 2 } => _localizationService.GetLocal("Common.Yesterday"),
                var td => _localizationService.GetLocal("Common.DaysAgo", (int)td.TotalDays)
            },
            () => _localizationService.GetLocal("Common.NeverSynced"));
}
