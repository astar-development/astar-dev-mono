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
    private readonly OneDriveAccount account;
    private readonly ISyncScheduler scheduler;
    private readonly IAccountRepository repository;

    /// <summary>Raw string account ID — unwrapped at the display boundary.</summary>
    public string AccountId => account.Id.Id;
    public string DisplayName => account.Profile.DisplayName;
    public string Email => account.Profile.Email;
    public string AccentHex => Accounts.AccountCardViewModel.PaletteHex(account.AccentIndex);
    public Avalonia.Media.Color AccentColor => Avalonia.Media.Color.Parse(AccentHex);

    public long QuotaTotal => account.Quota.TotalBytes;
    public long QuotaUsed => account.Quota.UsedBytes;
    public double StorageFraction => account.Quota.Fraction();
    public string StorageText => account.Quota.TotalBytes > 0
        ? $"{account.Quota.UsedBytes.FileSizeToText()} / {account.Quota.TotalBytes.FileSizeToText()}"
        : localizationService.GetLocal("Common.Unknown");

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

    private readonly ILocalizationService localizationService;
    private readonly IActivityItemViewModelFactory activityItemViewModelFactory;

    [ObservableProperty]
    public partial bool IsSyncing { get; set; }

    public bool IsHealthy => SyncState is SyncState.Idle && ConflictCount == 0;
    public bool HasEverSynced => account.LastSyncedAt?.Match(_ => true, () => false) ?? false;
    public string StatusLabel => (SyncState, ConflictCount) switch
    {
        (SyncState.Syncing, _) => localizationService.GetLocal("StatusBar.Syncing"),
        (SyncState.Error, _) => localizationService.GetLocal("StatusBar.Error"),
        (_, > 0) => ConflictCount == 1
            ? localizationService.GetLocal("StatusBar.Conflict", ConflictCount)
            : localizationService.GetLocal("StatusBar.Conflicts", ConflictCount),
        (SyncState.Pending, _) => localizationService.GetLocal("Dashboard.Pending"),
        _ => localizationService.GetLocal("StatusBar.Synced")
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpanderGlyph))]
    public partial bool IsExpanded { get; set; } = true;

    public string ExpanderGlyph => IsExpanded ? "▾" : "▸";

    public ObservableCollection<ActivityItemViewModel> RecentActivity { get; } = [];

    /// <summary>Localised "Storage" section heading.</summary>
    public string StorageHeadingText => localizationService.GetLocal("Dashboard.Storage");

    /// <summary>Localised "folders" stat label.</summary>
    public string FoldersLabelText => localizationService.GetLocal("Dashboard.Folders");

    /// <summary>Localised "conflicts" stat label.</summary>
    public string ConflictsLabelText => localizationService.GetLocal("Dashboard.Conflicts");

    /// <summary>Localised "last sync" stat label.</summary>
    public string LastSyncLabelText => localizationService.GetLocal("Dashboard.LastSync");

    /// <summary>Localised "Recent activity" section heading.</summary>
    public string RecentActivityText => localizationService.GetLocal("Dashboard.RecentActivity");

    /// <summary>Localised "No recent activity" empty state.</summary>
    public string NoRecentActivityText => localizationService.GetLocal("Dashboard.NoRecentActivity");

    /// <summary>Localised "Sync now" button label.</summary>
    public string SyncNowButtonText => localizationService.GetLocal("Dashboard.SyncNow");

    /// <summary>Localised "Cancel" button label.</summary>
    public string CancelButtonText => localizationService.GetLocal("Common.Cancel");

    public DashboardAccountViewModel(OneDriveAccount account, ISyncScheduler scheduler, IAccountRepository repository, ILocalizationService localizationService, IActivityItemViewModelFactory activityItemViewModelFactory)
    {
        this.account = account;
        this.scheduler = scheduler;
        FolderCount = account.SelectedFolderIds.Count;
        this.localizationService = localizationService;
        this.activityItemViewModelFactory = activityItemViewModelFactory;
        this.repository = repository;
        UpdateLastSyncText(SyncState.Idle);
    }

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private Task CancelSyncAsync() => scheduler.CancelAccountSyncAsync(account.Id.Id);

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        AddRecentActivity(activityItemViewModelFactory.Create(localizationService.GetLocal("Sync.Starting")));

        await repository.GetByIdAsync(account.Id, CancellationToken.None)
            .TapAsync(async entity =>
            {
                var fullAccount = new OneDriveAccount
                {
                    Id = entity.Id,
                    Profile = entity.Profile,
                    SyncConfig = entity.SyncConfig.LocalSyncPath.Value.Length > 0 ? Option.Some(entity.SyncConfig) : Option.None<AccountSyncConfig>(),
                    LastSyncedAt = entity.LastSyncedAt
                };
                await scheduler.TriggerAccountAsync(fullAccount);
            });
    }

    public void UpdateSyncState(SyncState state, int conflicts)
    {
        SyncState = state;
        ConflictCount = conflicts;
        IsSyncing = state == SyncState.Syncing;

        if (state is not (SyncState.Idle or SyncState.Completed or SyncState.NoSyncPathConfigured)) return;

        if (state is not SyncState.NoSyncPathConfigured)
            account.LastSyncedAt = Option.Some(DateTimeOffset.UtcNow);

        UpdateLastSyncText(state);
    }

    /// <summary>Updates the displayed storage quota. Call after a successful quota refresh from the Graph API.</summary>
    public void UpdateQuota(StorageQuota quota)
    {
        account.Quota = quota;
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
        syncState == SyncState.NoSyncPathConfigured ? localizationService.GetLocal("Dashboard.NoSyncPath") :
        account.LastSyncedAt is null ? localizationService.GetLocal("Common.NeverSynced") :
        account.LastSyncedAt.Match(
            lastSyncedAt => (DateTimeOffset.UtcNow - lastSyncedAt) switch
            {
                { TotalSeconds: < 60 } => localizationService.GetLocal("Common.JustNow"),
                { TotalMinutes: < 60 } td => localizationService.GetLocal("Common.MinutesAgo", (int)td.TotalMinutes),
                { TotalHours: < 24 } td => localizationService.GetLocal("Common.HoursAgo", (int)td.TotalHours),
                { TotalDays: < 2 } => localizationService.GetLocal("Common.Yesterday"),
                var td => localizationService.GetLocal("Common.DaysAgo", (int)td.TotalDays)
            },
            () => localizationService.GetLocal("Common.NeverSynced"));
}
