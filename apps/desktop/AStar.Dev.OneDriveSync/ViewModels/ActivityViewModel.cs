using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.OneDriveSync.Models;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class ActivityViewModel : ReactiveObject
{
    private bool _isLogTabActive = true;
    private bool _isConflictsTabActive;

    public bool IsLogTabActive
    {
        get => _isLogTabActive;
        set => this.RaiseAndSetIfChanged(ref _isLogTabActive, value);
    }

    public bool IsConflictsTabActive
    {
        get => _isConflictsTabActive;
        set => this.RaiseAndSetIfChanged(ref _isConflictsTabActive, value);
    }

    public bool HasLogItems => FilteredLog.Count > 0;
    public bool HasConflicts => Conflicts.Count > 0;
    public string ConflictBadgeText => Conflicts.Count.ToString();

    public ObservableCollection<ActivityItemViewModel> FilteredLog { get; } = [];
    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = [];

    public ICommand SwitchTabCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand ClearLogCommand { get; }

    public ActivityViewModel()
    {
        SwitchTabCommand = ReactiveCommand.Create<ActivityTab>(tab =>
        {
            IsLogTabActive       = tab == ActivityTab.Log;
            IsConflictsTabActive = tab == ActivityTab.Conflicts;
        });
        SetFilterCommand  = ReactiveCommand.Create<ActivityItemType?>(filter => { });
        ClearLogCommand   = ReactiveCommand.Create(() => FilteredLog.Clear());
    }
}
