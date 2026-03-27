using AStar.Dev.OneDriveSync.old.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AStar.Dev.Conflict.Resolution;

namespace AStar.Dev.OneDriveSync.old.Controls;

public partial class ConflictResolutionPanel : UserControl
{
    public ConflictResolutionPanel() => InitializeComponent();

    private void OnPolicyClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: ConflictPolicy policy } && DataContext is ConflictItemViewModel vm)
            vm.SelectedPolicy = policy;
    }
}
