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
}
