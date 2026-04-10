using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Dashboard;

public sealed partial class DashboardViewModel(ISyncScheduler scheduler, ILocalizationService localizationService, IAccountRepository accountRepository) : ObservableObject
{
    public ObservableCollection<DashboardAccountViewModel> AccountSections { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccounts))]
    public partial int TotalAccounts { get; set; }

    [ObservableProperty]
    public partial int TotalFolders { get; set; }

    [ObservableProperty]
    public partial int TotalConflicts { get; set; }

    [ObservableProperty]
    public partial string LastSyncText { get; set; } = "Never";

    [ObservableProperty]
    public partial bool AnyErrors { get; set; }

    [ObservableProperty]
    public partial bool AnySyncing { get; set; }

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

        var section = new DashboardAccountViewModel(account, scheduler, accountRepository, localizationService);

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

    public void UpdateAccountSyncState(string accountId, Accounts.AccountCardViewModel card)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        if(section is null)
            return;

        section.UpdateSyncState(card.SyncState, card.ConflictCount);

        RecalculateGlobals();
    }

    public void MarkSyncCompleted(string accountId)
    {
        var section = AccountSections.FirstOrDefault(s => s.AccountId == accountId);
        if(section is null)
            return;

        section.UpdateSyncState(SyncState.Completed, section.ConflictCount);
        section.AddRecentActivity(new ActivityItemViewModel { AccountId = accountId, FileName = "Sync complete", Type = ActivityItemType.Info });

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
