using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.Spikes.AvaloniaTray;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    // Hide instead of close so the tray icon remains active
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true; // prevent the window from being destroyed
        Hide();
        base.OnClosing(e);
    }

    private void OnHide(object? sender, RoutedEventArgs e) => Hide();
}
