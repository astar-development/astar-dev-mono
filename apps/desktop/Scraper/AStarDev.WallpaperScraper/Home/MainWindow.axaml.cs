using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Logging.Extensions;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
namespace AStarDev.WallpaperScraper.Home;

[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    // Parameterless constructor is required by the XAML previewer only; the app resolves the DI constructor.
    public MainWindow(){}

    public MainWindow(MainWindowViewModel viewModel, ILogger<MainWindow> logger)
    {
        LogMessage.Information(logger, "MainWindow initialized with ViewModel: {ViewModel}", typeof(MainWindowViewModel).Name);
        DataContext = viewModel;
        Width = viewModel.WindowWidth;
        Height = viewModel.WindowHeight;

        InitializeComponent();
    }
}
