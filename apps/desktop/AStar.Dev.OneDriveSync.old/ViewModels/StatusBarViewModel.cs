using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

public class StatusBarViewModel : ReactiveObject
{
    public bool HasAccount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}
