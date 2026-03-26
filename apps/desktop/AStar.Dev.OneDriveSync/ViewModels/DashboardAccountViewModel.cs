using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using AStar.Dev.OneDriveSync.Models;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class DashboardAccountViewModel : ReactiveObject
{
    private bool _isExpanded;
    private bool _isSyncing;

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
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public bool IsSyncing
    {
        get => _isSyncing;
        set => this.RaiseAndSetIfChanged(ref _isSyncing, value);
    }

    public string ExpanderGlyph => IsExpanded ? "▲" : "▼";

    public ICommand SyncNowCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ToggleExpandCommand { get; init; } = ReactiveCommand.Create(() => { });
}
