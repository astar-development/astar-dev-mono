using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class AccountCardViewModel : ReactiveObject
{
    public string AccountId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public Color AccentColor { get; init; }
    public string LastSyncText { get; init; } = string.Empty;
    public bool IsActive
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ICommand SelectCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand RemoveCommand { get; init; } = ReactiveCommand.Create(() => { });
}
