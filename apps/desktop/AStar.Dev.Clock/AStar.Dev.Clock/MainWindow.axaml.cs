using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStar.Dev.Clock;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
