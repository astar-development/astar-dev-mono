using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using AStar.Dev.OneDriveSync.old.Models;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

public class DashboardAccountViewModel : ReactiveObject
{
    public string AccountId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Color AccentColor { get; init; }
    public SyncState SyncState { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public double StorageFraction { get; init; }
    public string StorageText { get; init; } = string.Empty;
    public int FolderCount { get; init; }
    public int ConflictCount { get; init; }
    public string LastSyncText { get; init; } = string.Empty;
    public ObservableCollection<ActivityItemViewModel> RecentActivity { get; } = [];

    public bool IsExpanded
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsSyncing
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string ExpanderGlyph => IsExpanded ? "▲" : "▼";

    public ICommand SyncNowCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ToggleExpandCommand { get; init; } = ReactiveCommand.Create(() => { });
}
