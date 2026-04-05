using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.OneDrive.Sync.Client.Views;

public partial class FilesView : UserControl
{
    public FilesView() => InitializeComponent();

    private async void OnTabClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: string accountId } && DataContext is FilesViewModel vm)
        {
            await vm.ActivateAccountAsync(accountId);
        }
    }
}
