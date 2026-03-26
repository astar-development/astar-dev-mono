using System.Windows.Input;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

public class UpdateNotificationViewModel : ReactiveObject
{
    public string Title { get; init; } = "Update available";
    public string VersionText { get; init; } = string.Empty;
    public string BodyText { get; init; } = string.Empty;
    public bool IsForcedUpdate { get; init; }
    public string ForcedUpdateWarningText { get; init; } = string.Empty;
    public string InstallButtonLabel { get; init; } = "Install now";
    public bool ShowDeferCountdown { get; init; }
    public string DeferCountdownText { get; init; } = string.Empty;

    public bool IsSyncActive
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ICommand InstallCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand DeferCommand { get; init; } = ReactiveCommand.Create(() => { });
}
