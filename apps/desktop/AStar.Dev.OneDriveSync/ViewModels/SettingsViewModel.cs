using System.Collections.ObjectModel;
using AStar.Dev.Conflict.Resolution;
using AStar.Dev.OneDriveSync.Models;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private AppTheme _theme = AppTheme.System;
    private ConflictPolicy _defaultConflictPolicy = ConflictPolicy.Skip;
    private int _syncIntervalMinutes = 60;

    public AppTheme Theme
    {
        get => _theme;
        set => this.RaiseAndSetIfChanged(ref _theme, value);
    }

    public ConflictPolicy DefaultConflictPolicy
    {
        get => _defaultConflictPolicy;
        set => this.RaiseAndSetIfChanged(ref _defaultConflictPolicy, value);
    }

    public int SyncIntervalMinutes
    {
        get => _syncIntervalMinutes;
        set => this.RaiseAndSetIfChanged(ref _syncIntervalMinutes, value);
    }

    public ObservableCollection<ConflictPolicyOption> PolicyOptions { get; } =
    [
        new() { Policy = ConflictPolicy.LocalWins,  Label = "Local wins",  Description = "Keep the local file, discard the remote version." },
        new() { Policy = ConflictPolicy.RemoteWins, Label = "Remote wins", Description = "Keep the remote file, discard the local version." },
        new() { Policy = ConflictPolicy.KeepBoth,   Label = "Keep both",   Description = "Rename the conflicting copy and keep both files." },
        new() { Policy = ConflictPolicy.Skip,       Label = "Skip",        Description = "Defer this conflict to the pending queue." }
    ];

    public ObservableCollection<SyncIntervalOption> IntervalOptions { get; } =
    [
        new(5,  "5 min"),
        new(15, "15 min"),
        new(30, "30 min"),
        new(60, "1 hr")
    ];

    public ObservableCollection<AccountSyncSettingsViewModel> AccountSettings { get; } = [];
}
