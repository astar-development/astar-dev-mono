using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using Avalonia.Controls;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    public MainWindow(MainWindowViewModel vm, IOptions<ClientConfiguration> config)
    {
        InitializeComponent();
        DataContext = vm;
        Title = $"{config.Value.ApplicationName} - V{config.Value.ApplicationVersion}";
    }
}
