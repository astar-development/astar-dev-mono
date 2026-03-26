using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class StatusBarViewModel : ReactiveObject
{
    private bool _hasAccount;

    public bool HasAccount
    {
        get => _hasAccount;
        set => this.RaiseAndSetIfChanged(ref _hasAccount, value);
    }
}
