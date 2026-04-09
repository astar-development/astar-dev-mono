using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Sync.Client.Splash;

public partial class SplashWindow : Window
{
    private readonly SplashWindowViewModel _viewModel = new();

    public SplashWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    public void SetStatus(string status) => _viewModel.Status = status;
}
