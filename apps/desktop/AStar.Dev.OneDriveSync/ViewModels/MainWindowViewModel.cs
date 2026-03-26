using System.Windows.Input;
using ReactiveUI;
using AStar.Dev.OneDriveSync.Theming;
using AStar.Dev.OneDriveSync.Views;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly IThemeService _themeService;
    private NavSection _activeSection = NavSection.Dashboard;
    private object? _activeView;
    private bool _isLogViewerOpen;

    // Views — created once and reused
    private readonly DashboardView _dashboardView;
    private readonly FilesView _filesView;
    private readonly ActivityView _activityView;
    private readonly AccountsView _accountsView;
    private readonly SettingsView _settingsView;
    private readonly LogViewerView _logViewerView;

    public MainWindowViewModel(IThemeService themeService)
    {
        _themeService = themeService;

        // Initialise sub-ViewModels
        Accounts  = new AccountsViewModel();
        StatusBar = new StatusBarViewModel();

        _dashboardView  = new DashboardView  { DataContext = new DashboardViewModel() };
        _filesView      = new FilesView      { DataContext = new FilesViewModel() };
        _activityView   = new ActivityView   { DataContext = new ActivityViewModel() };
        _accountsView   = new AccountsView   { DataContext = this };
        _settingsView   = new SettingsView   { DataContext = new SettingsViewModel() };
        _logViewerView  = new LogViewerView  { DataContext = new LogViewerViewModel() };

        NavigateCommand    = ReactiveCommand.Create<NavSection>(Navigate);
        AddAccountCommand  = ReactiveCommand.Create(OpenAddAccountWizard);
        OpenLogViewerCommand = ReactiveCommand.Create(OpenLogViewer);

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
        get => _activeView;
        private set => this.RaiseAndSetIfChanged(ref _activeView, value);
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
            NavSection.Dashboard => _dashboardView,
            NavSection.Files     => _filesView,
            NavSection.Activity  => _activityView,
            NavSection.Accounts  => _accountsView,
            NavSection.Settings  => _settingsView,
            _                    => _dashboardView
        };

        RaiseNavActiveChanged();
    }

    private void OpenAddAccountWizard()
    {
        Navigate(NavSection.Accounts);
        Accounts.IsWizardVisible = true;
        Accounts.Wizard          = new AddAccountWizardViewModel();
    }

    private void OpenLogViewer()
    {
        _isLogViewerOpen = true;
        ActiveView       = _logViewerView;
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
