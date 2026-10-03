using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.WallpaperScraper.UI;

/// <summary>Asks the user to confirm an action. Closes with <c>true</c> when confirmed and <c>false</c> when cancelled; cancel is the default button so an accidental Enter does not confirm.</summary>
public sealed partial class ConfirmationWindow : Window
{
    /// <summary>Initializes the window for the design-time previewer.</summary>
    public ConfirmationWindow() => InitializeComponent();

    /// <summary>Initializes a new instance of the <see cref="ConfirmationWindow"/> class.</summary>
    /// <param name="title">The window title.</param>
    /// <param name="message">What the user is being asked to confirm.</param>
    /// <param name="confirmText">The text of the button that confirms.</param>
    public ConfirmationWindow(string title, string message, string confirmText)
    {
        InitializeComponent();
        Title = title;
        MessageTextBlock.Text = message;
        ConfirmButton.Content = confirmText;
    }

    public void Confirm(object? sender, RoutedEventArgs eventArgs) => Close(true);

    public void Cancel(object? sender, RoutedEventArgs eventArgs) => Close(false);
}
