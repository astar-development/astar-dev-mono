using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.Wallpaper.Scraper;

public partial class ConfirmDialog : Window
{
    // Parameterless constructor is required by the XAML previewer only; the app uses the message constructor.
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string message)
        : this()
    {
        MessageText.Text = message;
    }

    private void OnYesClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnNoClick(object? sender, RoutedEventArgs e) => Close(false);
}
