using System.Collections.ObjectModel;
using System.Globalization;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.ClockControls;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Autostart;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Theme;
using AStarDev.OneDriveSyncClient.Localization;
using Avalonia.Platform.Storage;
using AStar.Dev.Velopack.Publishing;
using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.OneDriveSyncClient.Settings;

public sealed partial class SettingsViewModel : LocalizedObservableObject
{
    private const string SyncIntervalMinutesMessage = "Settings.DeltaSyncInterval.Minutes";
    private readonly ISettingsService settingsService;
    private readonly IThemeService themeService;
    private readonly ISyncScheduler scheduler;
    private readonly IAccountRepository repository;
    private readonly ILocalizationService loc;
    private readonly IFolderPickerService folderPickerService;
    private readonly IAutostartService autostartService;
    private readonly IUpdateNotificationService updateNotificationService;
    private readonly bool isLoaded;
    private bool isRefreshing;

    public SettingsViewModel(ISettingsService settingsService, IThemeService themeService, ISyncScheduler scheduler, IAccountRepository repository, ILocalizationService loc, IFolderPickerService folderPickerService, IAutostartService autostartService, IUpdateNotificationService updateNotificationService) : base(loc)
    {
        this.settingsService = settingsService;
        this.themeService = themeService;
        this.scheduler = scheduler;
        this.repository = repository;
        this.loc = loc;
        this.folderPickerService = folderPickerService;
        this.autostartService = autostartService;
        this.updateNotificationService = updateNotificationService;
        Theme = settingsService.Current.Theme;
        DefaultConflictPolicy = settingsService.Current.DefaultConflictPolicy;
        SyncIntervalMinutes = settingsService.Current.SyncIntervalMinutes;
        ConcurrentWorkerCount = settingsService.Current.ConcurrentWorkerCount;
        LaunchAtStartup = settingsService.Current.LaunchAtStartup;
        ClockStyle = ClockStyleFactory.Create(settingsService.Current.ClockStyle);
        ShowClockSeconds = settingsService.Current.ShowClockSeconds;
        ClockStyleOptions = ClockStyleOptionFactory.Create(loc, ClockStyle);
        ThemeOptions = ThemeOptionFactory.Create(loc, Theme);
        IntervalOptions = BuildIntervalOptions();
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, DefaultConflictPolicy);
        WorkerCountOptions = WorkerCountOptionFactory.Create(loc, ConcurrentWorkerCount);
        LanguageOptions = LanguageOptionFactory.Create(loc);
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

    [ObservableProperty]
    public partial bool LaunchAtStartup { get; set; }

    partial void OnLaunchAtStartupChanged(bool value)
    {
        if (!isLoaded || isRefreshing)
            return;

        settingsService.Current.LaunchAtStartup = value;
        _ = settingsService.SaveAsync();
        _ = autostartService.Apply(value);
    }

    /// <summary>The outcome of the most recent update check, localised when it was produced.</summary>
    [ObservableProperty]
    public partial string UpdateStatusText { get; set; } = string.Empty;

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        UpdateStatusText = loc.GetLocal("Settings.Updates.Checking");
        var result = await updateNotificationService.CheckNowAsync();
        UpdateStatusText = result switch
        {
            UpdateFound found => loc.GetLocal("Settings.Updates.Found", found.Info.TargetFullRelease.Version.ToString()),
            UpToDate => loc.GetLocal("Settings.Updates.UpToDate"),
            NotInstalled => loc.GetLocal("Settings.Updates.NotInstalled"),
            CheckFailed failed => loc.GetLocal("Settings.Updates.Failed", failed.Reason),
            _ => string.Empty
        };
    }

    [ObservableProperty]
    public partial ClockStyle ClockStyle { get; set; }

    partial void OnClockStyleChanged(ClockStyle value)
    {
        ClockStyleOptions = ClockStyleOptionFactory.Create(loc, value);
        OnPropertyChanged(nameof(ClockStyleOptions));

        if (!isLoaded || isRefreshing)
            return;

        settingsService.Current.ClockStyle = value.ToKey();
        _ = settingsService.SaveAsync();
    }

    [ObservableProperty]
    public partial bool ShowClockSeconds { get; set; }

    partial void OnShowClockSecondsChanged(bool value)
    {
        if (!isLoaded || isRefreshing)
            return;

        settingsService.Current.ShowClockSeconds = value;
        _ = settingsService.SaveAsync();
    }

    /// <summary>The available clock style options, localised for the current culture.</summary>
    public IReadOnlyList<ClockStyleOption> ClockStyleOptions { get; private set; } = [];

    /// <summary>Localised clock section heading.</summary>
    public string ClockHeadingText => loc.GetLocal("Settings.Section.Clock");

    /// <summary>Localised clock style row label.</summary>
    public string ClockStyleLabelText => loc.GetLocal("Settings.Clock.Style");

    /// <summary>Localised clock style row description.</summary>
    public string ClockStyleDescriptionText => loc.GetLocal("Settings.Clock.Style.Description");

    /// <summary>Localised show seconds row label.</summary>
    public string ShowClockSecondsLabelText => loc.GetLocal("Settings.Clock.ShowSeconds");

    /// <summary>Localised show seconds row description.</summary>
    public string ShowClockSecondsDescriptionText => loc.GetLocal("Settings.Clock.ShowSeconds.Description");

    /// <summary>Localised label for the launch at startup row.</summary>
    public string LaunchAtStartupLabelText => loc.GetLocal("Settings.LaunchAtStartup.Label");

    /// <summary>Localised description for the launch at startup row.</summary>
    public string LaunchAtStartupDescriptionText => loc.GetLocal("Settings.LaunchAtStartup.Description");

    /// <summary>Localised label for the check for updates row.</summary>
    public string CheckForUpdatesLabelText => loc.GetLocal("Settings.Updates.Label");

    /// <summary>Localised description for the check for updates row.</summary>
    public string CheckForUpdatesDescriptionText => loc.GetLocal("Settings.Updates.Description");

    /// <summary>Localised text for the check for updates button.</summary>
    public string CheckForUpdatesButtonText => loc.GetLocal("Settings.Updates.Button");

    /// <summary>Localised page title.</summary>
    public string TitleText => loc.GetLocal("Settings.Title");

    /// <summary>Localised page description.</summary>
    public string DescriptionText => loc.GetLocal("Settings.Description");

    /// <summary>Localised appearance section heading.</summary>
    public string AppearanceHeadingText => loc.GetLocal("Settings.Section.Appearance");

    /// <summary>Localised theme row label.</summary>
    public string ThemeLabelText => loc.GetLocal("Settings.Theme");

    /// <summary>Localised theme row description.</summary>
    public string ThemeDescriptionText => loc.GetLocal("Settings.Theme.Description");

    /// <summary>Localised language section heading.</summary>
    public string LanguageHeadingText => loc.GetLocal("Settings.Section.Language");

    /// <summary>Localised display language row label.</summary>
    public string DisplayLanguageLabelText => loc.GetLocal("Settings.Language.DisplayLanguage");

    /// <summary>Localised sync policy section heading.</summary>
    public string SyncPolicyHeadingText => loc.GetLocal("Settings.Section.SyncPolicy");

    /// <summary>Localised default conflict policy row label.</summary>
    public string DefaultConflictPolicyLabelText => loc.GetLocal("Settings.DefaultConflictPolicy");

    /// <summary>Localised default conflict policy row description.</summary>
    public string DefaultConflictPolicyDescriptionText => loc.GetLocal("Settings.DefaultConflictPolicy.Description");

    /// <summary>Localised sync interval row label.</summary>
    public string SyncIntervalLabelText => loc.GetLocal("Settings.DeltaSyncInterval");

    /// <summary>Localised sync interval row description.</summary>
    public string SyncIntervalDescriptionText => loc.GetLocal("Settings.DeltaSyncInterval.Description");

    /// <summary>Localised concurrent workers row label.</summary>
    public string ConcurrentWorkersLabelText => loc.GetLocal("Settings.ConcurrentWorkers.Label");

    /// <summary>Localised concurrent workers row description.</summary>
    public string ConcurrentWorkersDescriptionText => loc.GetLocal("Settings.ConcurrentWorkers.Description");

    /// <summary>Localised account sync paths section heading.</summary>
    public string AccountSyncPathsHeadingText => loc.GetLocal("Settings.Section.AccountSyncPaths");

    /// <summary>Localised account sync paths section description.</summary>
    public string AccountSyncPathsDescriptionText => loc.GetLocal("Settings.Section.AccountSyncPaths.Description");

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
    public async Task BrowseForAccountFolderAsync(string accountId, IStorageProvider storageProvider, CancellationToken cancellationToken = default)
    {
        var account = AccountSettings.FirstOrDefault(a => a.AccountId == accountId);
        if (account is null)
            return;

        string? path = await folderPickerService.PickFolderAsync(storageProvider, loc.GetLocal("Settings.ChooseSyncFolder"), cancellationToken).ConfigureAwait(false);
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
        new(5,   loc.GetLocal(SyncIntervalMinutesMessage, 5), SyncIntervalMinutes == 5),
        new(15,  loc.GetLocal(SyncIntervalMinutesMessage, 15), SyncIntervalMinutes == 15),
        new(30,  loc.GetLocal(SyncIntervalMinutesMessage, 30), SyncIntervalMinutes == 30),
        new(60,  loc.GetLocal(SyncIntervalMinutesMessage, 60), SyncIntervalMinutes == 60),
        new(120, loc.GetLocal("Settings.Interval.Hours", 2), SyncIntervalMinutes == 120),
    ];

    private void OnSettingsServiceChanged(object? sender, AppSettings settings)
    {
        isRefreshing = true;
        Theme = settings.Theme;
        DefaultConflictPolicy = settings.DefaultConflictPolicy;
        SyncIntervalMinutes = settings.SyncIntervalMinutes;
        ConcurrentWorkerCount = settings.ConcurrentWorkerCount;
        LaunchAtStartup = settings.LaunchAtStartup;
        ClockStyle = ClockStyleFactory.Create(settings.ClockStyle);
        ShowClockSeconds = settings.ShowClockSeconds;
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

    /// <inheritdoc />
    protected override void OnCultureChanged()
    {
        ThemeOptions = ThemeOptionFactory.Create(loc, Theme);
        IntervalOptions = BuildIntervalOptions();
        PolicyOptions = ConflictPolicyOptionFactory.Create(loc, DefaultConflictPolicy);
        WorkerCountOptions = WorkerCountOptionFactory.Create(loc, ConcurrentWorkerCount);
        LanguageOptions = LanguageOptionFactory.Create(loc);
        base.OnCultureChanged();
    }

}
