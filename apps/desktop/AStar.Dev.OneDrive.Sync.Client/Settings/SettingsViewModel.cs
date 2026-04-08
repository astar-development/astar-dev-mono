using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

public sealed partial class SettingsViewModel(ISettingsService settingsService, IThemeService themeService, SyncScheduler scheduler, IAccountRepository repository) : ObservableObject
{
    [ObservableProperty]
    public partial AppTheme Theme { get; set; } = settingsService.Current.Theme;

    partial void OnThemeChanged(AppTheme value)
    {
        themeService.Apply(value);
        settingsService.Current.Theme = value;
        _ = settingsService.SaveAsync();
    }

    public IReadOnlyList<ThemeOption> ThemeOptions { get; } =
    [
        new(AppTheme.Light,  "Light"),
        new(AppTheme.Dark,   "Dark"),
        new(AppTheme.System, "System"),
    ];

    [ObservableProperty]
    public partial ConflictPolicy DefaultConflictPolicy { get; set; } =
        settingsService.Current.DefaultConflictPolicy;

    partial void OnDefaultConflictPolicyChanged(ConflictPolicy value)
    {
        settingsService.Current.DefaultConflictPolicy = value;
        _ = settingsService.SaveAsync();
    }

    [ObservableProperty]
    public partial int SyncIntervalMinutes { get; set; } =
        settingsService.Current.SyncIntervalMinutes;

    partial void OnSyncIntervalMinutesChanged(int value)
    {
        settingsService.Current.SyncIntervalMinutes = value;
        scheduler.SetInterval(TimeSpan.FromMinutes(value));
        _ = settingsService.SaveAsync();
    }

    public IReadOnlyList<SyncIntervalOption> IntervalOptions { get; } =
    [
        new(5,   "5 minutes"),
        new(15,  "15 minutes"),
        new(30,  "30 minutes"),
        new(60,  "60 minutes"),
        new(120, "2 hours"),
    ];

    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; } =
    [
        new(ConflictPolicy.Ignore,        "Ignore",          "Skip — leave both unchanged"),
        new(ConflictPolicy.KeepBoth,      "Keep both",       "Rename local, keep remote"),
        new(ConflictPolicy.LastWriteWins, "Last write wins", "Most recently modified wins"),
        new(ConflictPolicy.LocalWins,     "Local wins",      "Local always overwrites remote"),
        new(ConflictPolicy.RemoteWins,    "Remote wins",     "Remote always overwrites local"),
    ];

    public ObservableCollection<Accounts.AccountSyncSettingsViewModel> AccountSettings { get; } = [];

    public void LoadAccounts(IEnumerable<OneDriveAccount> accounts)
    {
        AccountSettings.Clear();
        foreach(var a in accounts)
            AccountSettings.Add(new Accounts.AccountSyncSettingsViewModel(a, repository));
    }

    public void AddAccount(OneDriveAccount account)
        => AccountSettings.Add(new Accounts.AccountSyncSettingsViewModel(account, repository));

    public void RemoveAccount(string accountId)
    {
        var vm = AccountSettings.FirstOrDefault(a => a.AccountId == accountId);
        if(vm is not null)
            _ = AccountSettings.Remove(vm);
    }
}
