using System.Diagnostics.CodeAnalysis;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Versioning;
using Avalonia.Controls;
using Microsoft.Extensions.Options;

namespace AStarDev.OneDriveSyncClient.Home;

[ExcludeFromCodeCoverage]
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
