using System.Windows.Input;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class InsecureTokenConsentViewModel : ReactiveObject
{
    public string AccountEmail { get; init; } = string.Empty;

    public bool RememberDecision
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ICommand AllowCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand CancelCommand { get; init; } = ReactiveCommand.Create(() => { });
}
