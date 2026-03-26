using System.Collections.ObjectModel;
using AStar.Dev.Conflict.Resolution;
using AStar.Dev.OneDriveSync.old.Logging;
using AStar.Dev.OneDriveSync.old.Models;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private readonly LoggingService _loggingService;

    public SettingsViewModel(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public AppTheme Theme
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = AppTheme.System;

    public ConflictPolicy DefaultConflictPolicy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = ConflictPolicy.Skip;

    public int SyncIntervalMinutes
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = 60;

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

    public void SetAccountDebugLogging(string accountId, bool enabled) => _loggingService.SetAccountDebugEnabled(accountId, enabled);

    public bool IsAccountDebugLogging(string accountId) => _loggingService.IsAccountDebugEnabled(accountId);
}
