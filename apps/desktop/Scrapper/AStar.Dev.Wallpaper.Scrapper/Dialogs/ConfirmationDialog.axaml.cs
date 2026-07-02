using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.Wallpaper.Scrapper.Dialogs;

public sealed partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void OnYesClicked(object? sender, RoutedEventArgs e) => Close(true);
    private void OnNoClicked(object? sender, RoutedEventArgs e) => Close(false);
}
