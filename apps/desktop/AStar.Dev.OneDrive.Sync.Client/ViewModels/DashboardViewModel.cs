using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class DashboardViewModel(SyncScheduler scheduler) : ObservableObject
{
    public ObservableCollection<DashboardAccountViewModel> AccountSections { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccounts))]
    private int _totalAccounts;

    [ObservableProperty] private int    _totalFolders;
    [ObservableProperty] private int    _totalConflicts;
    [ObservableProperty] private string _lastSyncText   = "Never";
    [ObservableProperty] private bool   _anyErrors;
    [ObservableProperty] private bool   _anySyncing;

    public bool HasAccounts => TotalAccounts > 0;

    public string OverallStatusText => (AnySyncing, AnyErrors, TotalConflicts) switch
    {
        (true, _, _) => "Syncing ...",
        (_, true, _) => "Error",
        (_, _, > 0) => $"{TotalConflicts} conflict{(TotalConflicts == 1 ? "" : "s")}",
        _ => "All synced"
    };

    public void AddAccount(OneDriveAccount account)
    {
        if(AccountSections.Any(s => s.AccountId == account.Id))
            return;

        var section = new DashboardAccountViewModel(account, scheduler, App.AccountRepository);

        AccountSections.Add(section);

        RecalculateGlobals();
    }

    public void RemoveAccount(string accountId)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        if(section is null)
            return;

        _ = AccountSections.Remove(section);

        RecalculateGlobals();
    }

    public void UpdateAccountSyncState(string accountId, AccountCardViewModel card)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        if(section is null)
            return;

        section.UpdateSyncState(card.SyncState, card.ConflictCount);

        RecalculateGlobals();
    }

    public void AddActivityItem(ActivityItemViewModel item)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == item.AccountId);
        section?.AddRecentActivity(item);
    }

    private void RecalculateGlobals()
    {
        TotalAccounts = AccountSections.Count;
        TotalFolders = AccountSections.Sum(s => s.FolderCount);
        TotalConflicts = AccountSections.Sum(s => s.ConflictCount);
        AnyErrors = AccountSections.Any(s => s.SyncState == SyncState.Error);
        AnySyncing = AccountSections.Any(s => s.SyncState == SyncState.Syncing);

        var mostRecent = AccountSections.FirstOrDefault(s => s.LastSyncText != "Never synced");

        LastSyncText = mostRecent?.LastSyncText ?? "Never";
        OnPropertyChanged(nameof(OverallStatusText));
    }
}
