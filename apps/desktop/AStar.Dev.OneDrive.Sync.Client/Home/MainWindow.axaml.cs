using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        vm.InitialiseAsync().GetAwaiter().GetResult();
    }
}
