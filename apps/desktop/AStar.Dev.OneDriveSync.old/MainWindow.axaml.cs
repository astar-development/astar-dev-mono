using AStar.Dev.OneDriveSync.old.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace AStar.Dev.OneDriveSync.old;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // LG-03: CTRL+SHIFT+ALT+L opens the hidden log viewer
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if(e.Key == Key.L
           && e.KeyModifiers.HasFlag(KeyModifiers.Control)
           && e.KeyModifiers.HasFlag(KeyModifiers.Shift)
           && e.KeyModifiers.HasFlag(KeyModifiers.Alt)
           && DataContext is MainWindowViewModel vm)
        {
            vm.OpenLogViewerCommand.Execute(null);
            e.Handled = true;
        }
    }
}
