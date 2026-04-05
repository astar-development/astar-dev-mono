using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.OneDrive.Sync.Client.Views;

public partial class ActivityView : UserControl
{
    public ActivityView() => InitializeComponent();

    private void OnLogTabClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is ActivityViewModel vm)
            vm.SwitchTabCommand.Execute(ActivityTab.Log);
    }

    private void OnConflictsTabClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is ActivityViewModel vm)
            vm.SwitchTabCommand.Execute(ActivityTab.Conflicts);
    }

    private void OnFilterAllClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is ActivityViewModel vm)
            vm.SetFilterCommand.Execute(null);
    }

    private void OnFilterDownloadsClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is ActivityViewModel vm)
            vm.SetFilterCommand.Execute(ActivityItemType.Downloaded);
    }

    private void OnFilterUploadsClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is ActivityViewModel vm)
            vm.SetFilterCommand.Execute(ActivityItemType.Uploaded);
    }

    private void OnFilterErrorsClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is ActivityViewModel vm)
            vm.SetFilterCommand.Execute(ActivityItemType.Error);
    }
}
