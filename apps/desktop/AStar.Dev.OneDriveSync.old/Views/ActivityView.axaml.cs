using AStar.Dev.OneDriveSync.old.Models;
using AStar.Dev.OneDriveSync.old.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AStar.Dev.Conflict.Resolution;

namespace AStar.Dev.OneDriveSync.old.Views;

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

    private void OnConflictCheckboxClick(object? sender, RoutedEventArgs e)
    {
        if(DataContext is ActivityViewModel vm)
            vm.NotifySelectionChanged();
    }

    private void OnBulkPolicyClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: ConflictPolicy policy } && DataContext is ActivityViewModel vm)
            vm.BulkPolicy = policy;
    }
}
