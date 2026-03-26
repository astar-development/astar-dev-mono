using AStar.Dev.Conflict.Resolution;
using AStar.Dev.OneDriveSync.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.OneDriveSync.Controls;

public partial class ConflictResolutionPanel : UserControl
{
    public ConflictResolutionPanel() => InitializeComponent();

    private void OnPolicyClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: ConflictPolicy policy } && DataContext is ConflictItemViewModel vm)
            vm.SelectedPolicy = policy;
    }
}
