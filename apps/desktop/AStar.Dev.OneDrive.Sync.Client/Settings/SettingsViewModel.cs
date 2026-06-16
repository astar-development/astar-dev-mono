using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Platform.Storage;
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
    private readonly IFolderPickerService folderPickerService;
    private bool isLoaded;
    private bool isRefreshing;

    public SettingsViewModel(ISettingsService settingsService, IThemeService themeService, ISyncScheduler scheduler, IAccountRepository repository, ILocalizationService loc, IFolderPickerService folderPickerService)
    {
        this.settingsService = settingsService;
        this.themeService = themeService;
        this.scheduler = scheduler;
        this.repository = repository;
        this.loc = loc;
        this.folderPickerService = folderPickerService;
        Theme = settingsService.Current.Theme;
        DefaultConflictPolicy = settingsService.Current.DefaultConflictPolicy;
        SyncIntervalMinutes = settingsService.Current.SyncIntervalMinutes;
        ConcurrentWorkerCount = settingsService.Current.ConcurrentWorkerCount;
        ThemeOptions = ThemeOptionFactory.Create(loc, Theme);
        IntervalOptions = BuildIntervalOptions();
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, DefaultConflictPolicy);
        WorkerCountOptions = WorkerCountOptionFactory.Create(loc, ConcurrentWorkerCount);
        LanguageOptions = LanguageOptionFactory.Create(loc);
        loc.CultureChanged += OnCultureChanged;
        settingsService.SettingsChanged += OnSettingsServiceChanged;
        isLoaded = true;
    }

    [ObservableProperty]
    public partial AppTheme Theme { get; set; }

    partial void OnThemeChanged(AppTheme value)
    {
        if (!isLoaded || isRefreshing)
            return;

        themeService.Apply(value);
        settingsService.Current.Theme = value;
        _ = settingsService.SaveAsync();
        ThemeOptions = ThemeOptionFactory.Create(loc, value);
        OnPropertyChanged(nameof(ThemeOptions));
    }

    /// <summary>The available theme options, localised for the current culture.</summary>
    public IReadOnlyList<ThemeOption> ThemeOptions { get; private set; }

    [ObservableProperty]
    public partial ConflictPolicy DefaultConflictPolicy { get; set; }

    partial void OnDefaultConflictPolicyChanged(ConflictPolicy value)
    {
        if (!isLoaded || isRefreshing)
            return;

        settingsService.Current.DefaultConflictPolicy = value;
        _ = settingsService.SaveAsync();
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, value);
        OnPropertyChanged(nameof(PolicyOptions));
    }

    [ObservableProperty]
    public partial int SyncIntervalMinutes { get; set; }

    partial void OnSyncIntervalMinutesChanged(int value)
    {
        if (!isLoaded || isRefreshing)
            return;

        settingsService.Current.SyncIntervalMinutes = value;
        scheduler.SetInterval(TimeSpan.FromMinutes(value));
        _ = settingsService.SaveAsync();
        IntervalOptions = BuildIntervalOptions();
        OnPropertyChanged(nameof(IntervalOptions));
    }

    [ObservableProperty]
    public partial int ConcurrentWorkerCount { get; set; }

    partial void OnConcurrentWorkerCountChanged(int value)
    {
        if (!isLoaded || isRefreshing)
            return;

        settingsService.Current.ConcurrentWorkerCount = value;
        _ = settingsService.SaveAsync();
        WorkerCountOptions = WorkerCountOptionFactory.Create(loc, value);
        OnPropertyChanged(nameof(WorkerCountOptions));
    }

    /// <summary>The available sync interval options, localised for the current culture.</summary>
    public IReadOnlyList<SyncIntervalOption> IntervalOptions { get; private set; }

    /// <summary>The available conflict policy options, localised for the current culture.</summary>
    public IReadOnlyList<ConflictPolicyOption> PolicyOptions { get; private set; }

    /// <summary>The available concurrent worker count options, localised for the current culture.</summary>
    public IReadOnlyList<WorkerCountOption> WorkerCountOptions { get; private set; }

    /// <summary>The available language options derived from the embedded localisation files.</summary>
    public IReadOnlyList<LanguageOption> LanguageOptions { get; private set; }

    /// <summary>Localised description for the language selection row.</summary>
    public string LanguageDescriptionText => loc.GetLocal("Settings.Language.Description");

    /// <summary>The per-account sync settings view models.</summary>
    public ObservableCollection<AccountSyncSettingsViewModel> AccountSettings { get; } = [];

    /// <summary>Switches the active locale, persists the choice, and raises <see cref="ILocalizationService.CultureChanged"/>.</summary>
    public async Task SelectCultureAsync(CultureInfo culture)
    {
        await loc.SetCultureAsync(culture).ConfigureAwait(false);
        settingsService.Current.Locale = culture.Name;
        await settingsService.SaveAsync().ConfigureAwait(false);
    }

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

    /// <summary>Opens a folder picker for the given account and updates <see cref="AccountSyncSettingsViewModel.LocalSyncPath"/> if the user selects a folder.</summary>
    public async Task BrowseForAccountFolderAsync(string accountId, IStorageProvider storageProvider, CancellationToken ct = default)
    {
        var account = AccountSettings.FirstOrDefault(a => a.AccountId == accountId);
        if (account is null)
            return;

        string? path = await folderPickerService.PickFolderAsync(storageProvider, "Choose local sync folder", ct).ConfigureAwait(false);
        if (path is not null)
            account.LocalSyncPath = path;
    }

    /// <summary>Sets <see cref="AccountSyncSettingsViewModel.LocalSyncPath"/> for the given account ID. Returns <c>false</c> if the account is not found.</summary>
    public bool TryApplyLocalSyncPath(string accountId, string path)
    {
        var account = AccountSettings.FirstOrDefault(a => a.AccountId == accountId);
        if (account is null)
            return false;

        account.LocalSyncPath = path;

        return true;
    }

    private IReadOnlyList<SyncIntervalOption> BuildIntervalOptions() =>
    [
        new(5,   loc.GetLocal("Settings.DeltaSyncInterval.Minutes", 5),   SyncIntervalMinutes == 5),
        new(15,  loc.GetLocal("Settings.DeltaSyncInterval.Minutes", 15),  SyncIntervalMinutes == 15),
        new(30,  loc.GetLocal("Settings.DeltaSyncInterval.Minutes", 30),  SyncIntervalMinutes == 30),
        new(60,  loc.GetLocal("Settings.DeltaSyncInterval.Minutes", 60),  SyncIntervalMinutes == 60),
        new(120, loc.GetLocal("Settings.Interval.Hours", 2),              SyncIntervalMinutes == 120),
    ];

    private void OnSettingsServiceChanged(object? sender, AppSettings settings)
    {
        isRefreshing = true;
        Theme = settings.Theme;
        DefaultConflictPolicy = settings.DefaultConflictPolicy;
        SyncIntervalMinutes = settings.SyncIntervalMinutes;
        ConcurrentWorkerCount = settings.ConcurrentWorkerCount;
        isRefreshing = false;
        ThemeOptions = ThemeOptionFactory.Create(loc, Theme);
        OnPropertyChanged(nameof(ThemeOptions));
        IntervalOptions = BuildIntervalOptions();
        OnPropertyChanged(nameof(IntervalOptions));
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, DefaultConflictPolicy);
        OnPropertyChanged(nameof(PolicyOptions));
        WorkerCountOptions = WorkerCountOptionFactory.Create(loc, ConcurrentWorkerCount);
        OnPropertyChanged(nameof(WorkerCountOptions));
    }

    private void OnCultureChanged(object? sender, CultureInfo culture)
    {
        ThemeOptions = ThemeOptionFactory.Create(loc, Theme);
        OnPropertyChanged(nameof(ThemeOptions));
        IntervalOptions = BuildIntervalOptions();
        OnPropertyChanged(nameof(IntervalOptions));
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, DefaultConflictPolicy);
        OnPropertyChanged(nameof(PolicyOptions));
        WorkerCountOptions = WorkerCountOptionFactory.Create(loc, ConcurrentWorkerCount);
        OnPropertyChanged(nameof(WorkerCountOptions));
        LanguageOptions = LanguageOptionFactory.Create(loc);
        OnPropertyChanged(nameof(LanguageOptions));
        OnPropertyChanged(nameof(LanguageDescriptionText));
    }
}
