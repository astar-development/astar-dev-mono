using System.Collections.ObjectModel;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

public class DashboardViewModel : ReactiveObject
{
    public int TotalAccounts { get; set; }
    public int TotalFolders { get; set; }
    public int TotalConflicts { get; set; }
    public string OverallStatusText { get; set; } = "Idle";
    public string LastSyncText { get; set; } = "Never";
    public bool HasAccounts => AccountSections.Count > 0;
    public ObservableCollection<DashboardAccountViewModel> AccountSections { get; } = [];
}
