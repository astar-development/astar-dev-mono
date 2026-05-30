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
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc);
        loc.CultureChanged += (_, _) => { PolicyOptions = ConflictPolicyOptionFactory.Create(this.loc); OnPropertyChanged(nameof(PolicyOptions)); };
    }

    [ObservableProperty]
    public partial AppTheme Theme { get; set; }

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

    public IReadOnlyList<SyncIntervalOption> IntervalOptions { get; } =
    [
        new(5,   "5 minutes"),
        new(15,  "15 minutes"),
        new(30,  "30 minutes"),
        new(60,  "60 minutes"),
        new(120, "2 hours"),
    ];

    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; private set; }

    public ObservableCollection<AccountSyncSettingsViewModel> AccountSettings { get; } = [];

    public void LoadAccounts(IEnumerable<OneDriveAccount> accounts)
    {
        AccountSettings.Clear();
        foreach (var a in accounts)
            AccountSettings.Add(new AccountSyncSettingsViewModel(a, repository, loc));
    }

    public void AddAccount(OneDriveAccount account)
        => AccountSettings.Add(new AccountSyncSettingsViewModel(account, repository, loc));

    public void RemoveAccount(string accountId)
    {
        var vm = AccountSettings.FirstOrDefault(a => a.AccountId == accountId);
        if (vm is not null)
            _ = AccountSettings.Remove(vm);
    }
}
