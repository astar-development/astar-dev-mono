using System.Windows.Input;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

public sealed class NavItemViewModel : ViewModelBase
{
    public required NavSection Section { get; init; }
    public required string Label { get; init; }
    public required bool IsFeatureEnabled { get; init; }
    public required string Tooltip { get; init; }
    public required ICommand NavigateCommand { get; init; }
    public required string IconPath { get; init; }

    public bool IsActive
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Live badge count shown on the nav rail icon; 0 hides the badge.</summary>
    public int BadgeCount
    {
        get;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(HasBadge));
        }
    }

    /// <summary>Whether the badge should be visible.</summary>
    public bool HasBadge => BadgeCount > 0;
}
