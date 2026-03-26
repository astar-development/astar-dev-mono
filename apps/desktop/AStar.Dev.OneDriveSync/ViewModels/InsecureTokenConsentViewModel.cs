using System.Windows.Input;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class InsecureTokenConsentViewModel : ReactiveObject
{
    private bool _rememberDecision;

    public string AccountEmail { get; init; } = string.Empty;

    public bool RememberDecision
    {
        get => _rememberDecision;
        set => this.RaiseAndSetIfChanged(ref _rememberDecision, value);
    }

    public ICommand AllowCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand CancelCommand { get; init; } = ReactiveCommand.Create(() => { });
}
