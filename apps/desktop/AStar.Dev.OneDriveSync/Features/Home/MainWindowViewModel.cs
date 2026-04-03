using System.Collections.Generic;
using System.Linq;
using AStar.Dev.OneDriveSync.Infrastructure;
using System.Reactive;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.OneDriveSync.Infrastructure.Shell;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Home;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService          _navigationService;
    private readonly IFeatureAvailabilityService _featureAvailability;
    private readonly ILocalisationService        _localisationService;
    private readonly IReadOnlyList<NavItemViewModel> _allNavItems;

    // Material Design 24x24 icon paths (viewBox 0 0 24 24)
    private const string DashboardIconPath = "M3,3 H10 V10 H3 V3 Z M14,3 H21 V10 H14 V3 Z M3,14 H10 V21 H3 V14 Z M14,14 H21 V21 H14 V14 Z";
    private const string AccountsIconPath  = "M12,12 C14.21,12 16,10.21 16,8 C16,5.79 14.21,4 12,4 C9.79,4 8,5.79 8,8 C8,10.21 9.79,12 12,12 Z M12,14 C9.33,14 4,15.34 4,18 V20 H20 V18 C20,15.34 14.67,14 12,14 Z";
    private const string ActivityIconPath  = "M13,3 H11 V13 H13 V3 Z M18.66,4.34 L17.24,5.76 C18.99,7.5 20,9.86 20,12.5 C20,17.19 16.19,21 11.5,21 C6.81,21 3,17.19 3,12.5 C3,8.08 6.41,4.42 10.75,4.04 V2.02 C5.31,2.42 1,7.02 1,12.5 C1,18.3 5.7,23 11.5,23 C17.3,23 22,18.3 22,12.5 C22,9.27 20.6,6.36 18.66,4.34 Z";
    private const string ConflictsIconPath = "M12,2 C6.48,2 2,6.48 2,12 C2,17.52 6.48,22 12,22 C17.52,22 22,17.52 22,12 C22,6.48 17.52,2 12,2 Z M13,17 H11 V15 H13 V17 Z M13,13 H11 V7 H13 V13 Z";
    private const string LogViewerIconPath = "M3,5 H21 V7 H3 V5 Z M3,9 H21 V11 H3 V9 Z M3,13 H17 V15 H3 V13 Z M3,17 H21 V19 H3 V17 Z";
    private const string SettingsIconPath  = "M19.14,12.94 C19.18,12.64 19.2,12.33 19.2,12 C19.2,11.68 19.18,11.36 19.13,11.06 L21.16,9.48 C21.34,9.34 21.39,9.07 21.28,8.87 L19.36,5.55 C19.24,5.33 18.99,5.26 18.77,5.33 L16.38,6.29 C15.88,5.91 15.35,5.59 14.76,5.35 L14.4,2.81 C14.36,2.57 14.15,2.4 13.9,2.4 H10.1 C9.85,2.4 9.65,2.57 9.61,2.81 L9.25,5.35 C8.66,5.59 8.12,5.92 7.63,6.29 L5.24,5.33 C5.01,5.25 4.76,5.33 4.64,5.55 L2.72,8.87 C2.6,9.08 2.65,9.34 2.84,9.48 L4.87,11.06 C4.82,11.36 4.8,11.69 4.8,12 C4.8,12.31 4.82,12.64 4.87,12.94 L2.84,14.52 C2.66,14.66 2.6,14.93 2.72,15.13 L4.64,18.45 C4.76,18.67 5.01,18.74 5.24,18.67 L7.63,17.71 C8.13,18.09 8.66,18.41 9.25,18.65 L9.61,21.19 C9.65,21.43 9.85,21.6 10.1,21.6 H13.9 C14.15,21.6 14.36,21.43 14.39,21.19 L14.75,18.65 C15.34,18.41 15.88,18.08 16.37,17.71 L18.76,18.67 C18.99,18.75 19.24,18.67 19.36,18.45 L21.28,15.13 C21.4,14.91 21.34,14.66 21.15,14.52 L19.14,12.94 Z M12,15.6 C10.02,15.6 8.4,13.98 8.4,12 C8.4,10.02 10.02,8.4 12,8.4 C13.98,8.4 15.6,10.02 15.6,12 C15.6,13.98 13.98,15.6 12,15.6 Z";
    private const string HelpIconPath      = "M11,18 H13 V16 H11 V18 Z M12,2 C6.48,2 2,6.48 2,12 C2,17.52 6.48,22 12,22 C17.52,22 22,17.52 22,12 C22,6.48 17.52,2 12,2 Z M12,20 C7.59,20 4,16.41 4,12 C4,7.59 7.59,4 12,4 C16.41,4 20,7.59 20,12 C20,16.41 16.41,20 12,20 Z M12,6 C9.79,6 8,7.79 8,10 H10 C10,8.9 10.9,8 12,8 C13.1,8 14,8.9 14,10 C14,12 11,11.75 11,15 H13 C13,12.75 16,12.5 16,10 C16,7.79 14.21,6 12,6 Z";
    private const string AboutIconPath     = "M11,17 H13 V11 H11 V17 Z M11,9 H13 V7 H11 V9 Z M12,2 C6.48,2 2,6.48 2,12 C2,17.52 6.48,22 12,22 C17.52,22 22,17.52 22,12 C22,6.48 17.52,2 12,2 Z M12,20 C7.59,20 4,16.41 4,12 C4,7.59 7.59,4 12,4 C16.41,4 20,7.59 20,12 C20,16.41 16.41,20 12,20 Z";

    public MainWindowViewModel(INavigationService navigationService, IFeatureAvailabilityService featureAvailability, ILocalisationService localisationService, IShellNavigator shellNavigator)
    {
        _navigationService   = navigationService;
        _featureAvailability = featureAvailability;
        _localisationService = localisationService;

        NavigateCommand = ReactiveCommand.Create<NavSection>(Navigate);
        _allNavItems    = BuildNavItems();
        NavItems        = _allNavItems;
        TopNavItems     = [.. _allNavItems.Take(5)];
        BottomNavItems  = [.. _allNavItems.Skip(5)];

        shellNavigator.Subscribe(section => Navigate(section));
    }

    public bool IsLoading
    {
        get;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
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
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(HasStartupError));
        }
    }

    public bool HasStartupError => StartupErrorMessage is not null;

    public IReadOnlyList<NavItemViewModel> NavItems { get; }
    public IReadOnlyList<NavItemViewModel> TopNavItems { get; }
    public IReadOnlyList<NavItemViewModel> BottomNavItems { get; }

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

        UpdateActiveNavItem(section);
        SelectedSection = section;
        ActiveView      = _navigationService.ResolveView(section);
    }

    private void UpdateActiveNavItem(NavSection activeSection)
    {
        foreach (var item in _allNavItems)
            item.IsActive = item.Section == activeSection;
    }

    private IReadOnlyList<NavItemViewModel> BuildNavItems() =>
    [
        CreateNavItem(NavSection.Dashboard, "Nav_Dashboard", DashboardIconPath),
        CreateNavItem(NavSection.Accounts,  "Nav_Accounts",  AccountsIconPath),
        CreateNavItem(NavSection.Activity,  "Nav_Activity",  ActivityIconPath),
        CreateNavItem(NavSection.Conflicts, "Nav_Conflicts", ConflictsIconPath),
        CreateNavItem(NavSection.LogViewer, "Nav_LogViewer", LogViewerIconPath),
        CreateNavItem(NavSection.Settings,  "Nav_Settings",  SettingsIconPath),
        CreateNavItem(NavSection.Help,      "Nav_Help",      HelpIconPath),
        CreateNavItem(NavSection.About,     "Nav_About",     AboutIconPath),
    ];

    private NavItemViewModel CreateNavItem(NavSection section, string labelKey, string iconPath)
    {
        bool   isEnabled    = _featureAvailability.IsAvailable(section);
        string label        = _localisationService.GetString(labelKey);
        string comingSoon   = _localisationService.GetString("Nav_ComingSoonSuffix");

        return new NavItemViewModel
        {
            Section          = section,
            Label            = label,
            IsFeatureEnabled = isEnabled,
            Tooltip          = isEnabled ? label : $"{label} {comingSoon}",
            NavigateCommand  = NavigateCommand,
            IconPath         = iconPath,
        };
    }
}
