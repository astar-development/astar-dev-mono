using AStar.Dev.OneDriveSync.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.OneDriveSync.Views;

public partial class FilesView : UserControl
{
    public FilesView() => InitializeComponent();

    private async void OnTabClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: string accountId } && DataContext is FilesViewModel vm)
            await vm.ActivateAccountAsync(accountId);
    }
}
