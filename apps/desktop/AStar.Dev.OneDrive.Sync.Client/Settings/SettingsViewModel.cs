using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService settingsService;
    private readonly IThemeService themeService;
    private readonly ISyncScheduler scheduler;
    private readonly IAccountRepository repository;
    private readonly ILocalizationService loc;

    public SettingsViewModel(ISettingsService settingsService, IThemeService themeService, ISyncScheduler scheduler, IAccountRepository repository, ILocalizationService loc)
    {
        this.settingsService = settingsService;
        this.themeService = themeService;
        this.scheduler = scheduler;
        this.repository = repository;
        this.loc = loc;
        Theme = settingsService.Current.Theme;
        DefaultConflictPolicy = settingsService.Current.DefaultConflictPolicy;
        SyncIntervalMinutes = settingsService.Current.SyncIntervalMinutes;
        ThemeOptions = ThemeOptionFactory.Create(loc);
        IntervalOptions = BuildIntervalOptions();
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc);
        loc.CultureChanged += OnCultureChanged;
    }

    [ObservableProperty]
    public partial AppTheme Theme { get; set; }

    partial void OnThemeChanged(AppTheme value)
    {
        themeService.Apply(value);
        settingsService.Current.Theme = value;
        _ = settingsService.SaveAsync();
    }

    /// <summary>The available theme options, localised for the current culture.</summary>
    public IReadOnlyList<ThemeOption> ThemeOptions { get; private set; }

    [ObservableProperty]
    public partial ConflictPolicy DefaultConflictPolicy { get; set; }

    partial void OnDefaultConflictPolicyChanged(ConflictPolicy value)
    {
        settingsService.Current.DefaultConflictPolicy = value;
        _ = settingsService.SaveAsync();
    }

    [ObservableProperty]
    public partial int SyncIntervalMinutes { get; set; }

    partial void OnSyncIntervalMinutesChanged(int value)
    {
        settingsService.Current.SyncIntervalMinutes = value;
        scheduler.SetInterval(TimeSpan.FromMinutes(value));
        _ = settingsService.SaveAsync();
    }

    /// <summary>The available sync interval options, localised for the current culture.</summary>
    public IReadOnlyList<SyncIntervalOption> IntervalOptions { get; private set; }

    /// <summary>The available conflict policy options, localised for the current culture.</summary>
    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; private set; }

    /// <summary>The per-account sync settings view models.</summary>
    public ObservableCollection<AccountSyncSettingsViewModel> AccountSettings { get; } = [];

    /// <summary>Loads the account settings view models from the given accounts, replacing any existing entries.</summary>
    public void LoadAccounts(IEnumerable<OneDriveAccount> accounts)
    {
        AccountSettings.Clear();
        foreach (var account in accounts)
            AccountSettings.Add(new AccountSyncSettingsViewModel(account, repository, loc));
    }

    /// <summary>Adds a new account settings view model for the given account.</summary>
    public void AddAccount(OneDriveAccount account)
        => AccountSettings.Add(new AccountSyncSettingsViewModel(account, repository, loc));

    /// <summary>Removes the account settings view model for the given account ID.</summary>
    public void RemoveAccount(string accountId)
    {
        var vm = AccountSettings.FirstOrDefault(a => a.AccountId == accountId);
        if (vm is not null)
            _ = AccountSettings.Remove(vm);
    }

    private IReadOnlyList<SyncIntervalOption> BuildIntervalOptions() =>
    [
        new(5,   loc.GetLocal("Settings.DeltaSyncInterval.Minutes", 5)),
        new(15,  loc.GetLocal("Settings.DeltaSyncInterval.Minutes", 15)),
        new(30,  loc.GetLocal("Settings.DeltaSyncInterval.Minutes", 30)),
        new(60,  loc.GetLocal("Settings.DeltaSyncInterval.Minutes", 60)),
        new(120, loc.GetLocal("Settings.Interval.Hours", 2)),
    ];

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo culture)
    {
        ThemeOptions = ThemeOptionFactory.Create(loc);
        OnPropertyChanged(nameof(ThemeOptions));
        IntervalOptions = BuildIntervalOptions();
        OnPropertyChanged(nameof(IntervalOptions));
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc);
        OnPropertyChanged(nameof(PolicyOptions));
    }
}
