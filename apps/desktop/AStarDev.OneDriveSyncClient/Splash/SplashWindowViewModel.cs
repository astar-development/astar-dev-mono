using AStarDev.OneDriveSyncClient.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AStarDev.OneDriveSyncClient.Splash;

public partial class SplashWindowViewModel : ViewModelBase
{
    public string AppName { get; init; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;
}
