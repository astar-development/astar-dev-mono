using AStar.Dev.OneDrive.Sync.Client.Infrastructure.ApplicationConfiguration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Versioning;
using Avalonia.Controls;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    public MainWindow(MainWindowViewModel vm, IOptions<ClientConfiguration> config, IApplicationVersionProvider versionProvider)
    {
        InitializeComponent();
        DataContext = vm;
        Title = $"{config.Value.ApplicationName} - V{versionProvider.CurrentVersion}";
    }
}
