using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class AccountSyncSettingsViewModel : ReactiveObject
{
    public string AccountId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Color AccentColor { get; init; }

    public string LocalSyncPath
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>LG-01: Per-account debug logging toggle.</summary>
    public bool IsDebugLoggingEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ICommand SaveCommand { get; init; } = ReactiveCommand.Create(() => { });
}
