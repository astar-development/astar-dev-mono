using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class AccountSyncSettingsViewModel : ReactiveObject
{
    private string _localSyncPath = string.Empty;
    private bool _isDebugLoggingEnabled;

    public string AccountId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Color AccentColor { get; init; }

    public string LocalSyncPath
    {
        get => _localSyncPath;
        set => this.RaiseAndSetIfChanged(ref _localSyncPath, value);
    }

    /// <summary>LG-01: Per-account debug logging toggle.</summary>
    public bool IsDebugLoggingEnabled
    {
        get => _isDebugLoggingEnabled;
        set => this.RaiseAndSetIfChanged(ref _isDebugLoggingEnabled, value);
    }

    public ICommand SaveCommand { get; init; } = ReactiveCommand.Create(() => { });
}
