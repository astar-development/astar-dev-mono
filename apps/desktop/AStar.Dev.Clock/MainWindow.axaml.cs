using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AStar.Dev.Clock;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLight(object? sender, RoutedEventArgs e)
        => (Application.Current as App)?.SetTheme(ThemeVariant.Light);

    private void OnDark(object? sender, RoutedEventArgs e)
        => (Application.Current as App)?.SetTheme(ThemeVariant.Dark);

    private void OnAuto(object? sender, RoutedEventArgs e)
        => (Application.Current as App)?.SetTheme(null);
}
