using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Shown instead of the main window when the application's services could not be built.</summary>
public sealed partial class StartupErrorWindow : Window
{
    /// <summary>Initializes the window for the design-time previewer.</summary>
    public StartupErrorWindow() => InitializeComponent();

    /// <summary>Initializes a new instance of the <see cref="StartupErrorWindow"/> class describing <paramref name="exception"/>.</summary>
    /// <param name="exception">The failure that stopped the application starting.</param>
    public StartupErrorWindow(Exception exception)
    {
        InitializeComponent();
        ErrorText.Text = $"{exception.GetType().Name}: {exception.Message}";
    }

    public void Exit(object? sender, RoutedEventArgs eventArgs) => Close();
}
