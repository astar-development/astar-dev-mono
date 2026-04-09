using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Splash;

public partial class SplashWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _status = string.Empty;
}
