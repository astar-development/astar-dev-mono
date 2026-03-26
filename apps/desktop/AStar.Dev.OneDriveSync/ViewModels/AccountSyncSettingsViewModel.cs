using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class AccountSyncSettingsViewModel : ReactiveObject
{
    private string _localSyncPath = string.Empty;

    public string AccountId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Color AccentColor { get; init; }

    public string LocalSyncPath
    {
        get => _localSyncPath;
        set => this.RaiseAndSetIfChanged(ref _localSyncPath, value);
    }

    public ICommand SaveCommand { get; init; } = ReactiveCommand.Create(() => { });
}
