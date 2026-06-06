using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Splash;

public partial class SplashWindowViewModel : ViewModelBase
{
    public string AppName { get; init; } = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;
}
