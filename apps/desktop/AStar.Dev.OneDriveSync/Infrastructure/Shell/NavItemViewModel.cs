using System.Windows.Input;

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

public sealed record NavItemViewModel
{
    public required NavSection Section { get; init; }
    public required string Label { get; init; }
    public required bool IsFeatureEnabled { get; init; }
    public required string Tooltip { get; init; }
    public required ICommand NavigateCommand { get; init; }
}
