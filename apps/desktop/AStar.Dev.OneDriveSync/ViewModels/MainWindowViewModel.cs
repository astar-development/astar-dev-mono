using System.Windows.Input;
using ReactiveUI;
using AStar.Dev.OneDriveSync.Logging;
using AStar.Dev.OneDriveSync.Theming;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly IThemeService _themeService;
    private NavSection _activeSection = NavSection.Dashboard;
    private bool _isLogViewerOpen;

    // Sub-ViewModels — created once and reused
    private readonly DashboardViewModel _dashboardVm;
    private readonly FilesViewModel _filesVm;
    private readonly ActivityViewModel _activityVm;
    private readonly SettingsViewModel _settingsVm;
    private readonly LogViewerViewModel _logViewerVm;

    public MainWindowViewModel(IThemeService themeService, LoggingService loggingService)
    {
        _themeService = themeService;

        // Initialise sub-ViewModels
        Accounts  = new AccountsViewModel();
        StatusBar = new StatusBarViewModel();

        _dashboardVm  = new DashboardViewModel();
        _filesVm      = new FilesViewModel();
        _activityVm   = new ActivityViewModel();
        _settingsVm   = new SettingsViewModel(loggingService);
        _logViewerVm  = new LogViewerViewModel(loggingService.Sink);

        NavigateCommand      = ReactiveCommand.Create<NavSection>(Navigate);
        AddAccountCommand    = ReactiveCommand.Create(OpenAddAccountWizard);
        OpenLogViewerCommand = ReactiveCommand.Create(OpenLogViewer);

        _selectedThemeOption = ThemeOptions.First(o => o.Mode == themeService.CurrentMode);

        // Start on Dashboard
        Navigate(NavSection.Dashboard);
    }

    // ── Sub-ViewModels ─────────────────────────────────────────────────────
    public AccountsViewModel  Accounts  { get; }
    public StatusBarViewModel StatusBar { get; }

    // ── Navigation state ──────────────────────────────────────────────────
    public bool IsDashboardActive => _activeSection == NavSection.Dashboard && !_isLogViewerOpen;
    public bool IsFilesActive     => _activeSection == NavSection.Files     && !_isLogViewerOpen;
    public bool IsActivityActive  => _activeSection == NavSection.Activity  && !_isLogViewerOpen;
    public bool IsAccountsActive  => _activeSection == NavSection.Accounts  && !_isLogViewerOpen;
    public bool IsSettingsActive  => _activeSection == NavSection.Settings  && !_isLogViewerOpen;

    public object? ActiveView
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────
    public ICommand NavigateCommand { get; }
    public ICommand AddAccountCommand { get; }
    public ICommand OpenLogViewerCommand { get; }

    // ── Theme (kept from original scaffold for Settings compatibility) ────
    public IReadOnlyList<ThemeOption> ThemeOptions { get; } =
    [
        new ThemeOption(ThemeMode.Light, "Light"),
        new ThemeOption(ThemeMode.Dark,  "Dark"),
        new ThemeOption(ThemeMode.Auto,  "System")
    ];

    private ThemeOption _selectedThemeOption = null!;
    public ThemeOption SelectedThemeOption
    {
        get => _selectedThemeOption;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedThemeOption, value);
            _themeService.Apply(value.Mode);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────
    private void Navigate(NavSection section)
    {
        _activeSection    = section;
        _isLogViewerOpen  = false;

        ActiveView = section switch
        {
            NavSection.Dashboard => _dashboardVm,
            NavSection.Files     => _filesVm,
            NavSection.Activity  => _activityVm,
            NavSection.Accounts  => (object)Accounts,
            NavSection.Settings  => _settingsVm,
            _                    => _dashboardVm
        };

        RaiseNavActiveChanged();
    }

    private void OpenAddAccountWizard()
    {
        Navigate(NavSection.Accounts);
        Accounts.AddAccountCommand.Execute(null);
    }

    private void OpenLogViewer()
    {
        _isLogViewerOpen = true;
        ActiveView       = _logViewerVm;
        RaiseNavActiveChanged();
    }

    private void RaiseNavActiveChanged()
    {
        this.RaisePropertyChanged(nameof(IsDashboardActive));
        this.RaisePropertyChanged(nameof(IsFilesActive));
        this.RaisePropertyChanged(nameof(IsActivityActive));
        this.RaisePropertyChanged(nameof(IsAccountsActive));
        this.RaisePropertyChanged(nameof(IsSettingsActive));
    }
}
