using AStar.Dev.OneDriveSync.Infrastructure;
using System.Reactive;
using System.Windows.Input;
using AStar.Dev.OneDriveSync.Infrastructure.Shell;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Home;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IFeatureAvailabilityService _featureAvailability;

    public MainWindowViewModel(INavigationService navigationService, IFeatureAvailabilityService featureAvailability)
    {
        _navigationService = navigationService;
        _featureAvailability = featureAvailability;

        NavigateCommand = ReactiveCommand.Create<NavSection>(Navigate);
        NavItems = BuildNavItems();
    }

    public bool IsLoading
    {
        get;
        private set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(IsNavEnabled));
        }
    } = true;

    public bool IsNavEnabled => !IsLoading;

    public NavSection SelectedSection
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ViewModelBase? ActiveView
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? StartupErrorMessage
    {
        get;
        private set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(HasStartupError));
        }
    }

    public bool HasStartupError => StartupErrorMessage is not null;

    public IReadOnlyList<NavItemViewModel> NavItems { get; }

    public ReactiveCommand<NavSection, Unit> NavigateCommand { get; }

    internal void CompleteStartup()
    {
        Navigate(NavSection.Dashboard);
        IsLoading = false;
    }

    internal void SetStartupError(string message) => StartupErrorMessage = message;

    private void Navigate(NavSection section)
    {
        if (!_featureAvailability.IsAvailable(section))
            return;

        SelectedSection = section;
        ActiveView = _navigationService.ResolveView(section);
    }

    private IReadOnlyList<NavItemViewModel> BuildNavItems() =>
    [
        CreateNavItem(NavSection.Dashboard, "Dashboard"),
        CreateNavItem(NavSection.Accounts,  "Accounts"),
        CreateNavItem(NavSection.Activity,  "Activity"),
        CreateNavItem(NavSection.Conflicts, "Conflicts"),
        CreateNavItem(NavSection.LogViewer, "Log Viewer"),
        CreateNavItem(NavSection.Settings,  "Settings"),
        CreateNavItem(NavSection.Help,      "Help"),
        CreateNavItem(NavSection.About,     "About"),
    ];

    private NavItemViewModel CreateNavItem(NavSection section, string label)
    {
        var isEnabled = _featureAvailability.IsAvailable(section);

        return new NavItemViewModel
        {
            Section          = section,
            Label            = label,
            IsFeatureEnabled = isEnabled,
            Tooltip          = isEnabled ? label : $"{label} — Coming soon",
            NavigateCommand  = NavigateCommand,
        };
    }
}
