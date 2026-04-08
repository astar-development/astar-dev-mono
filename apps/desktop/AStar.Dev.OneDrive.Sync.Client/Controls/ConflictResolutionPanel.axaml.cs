using AStar.Dev.OneDrive.Sync.Client.Models;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ConflictItemViewModel = AStar.Dev.OneDrive.Sync.Client.Conflicts.ConflictItemViewModel;

namespace AStar.Dev.OneDrive.Sync.Client.Controls;

public partial class ConflictResolutionPanel : UserControl
{
    public ConflictResolutionPanel() => InitializeComponent();

    private void OnPolicyClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button { Tag: ConflictPolicy policy } && DataContext is ConflictItemViewModel vm)
        {
            vm.SelectedPolicy = policy;
        }
    }
}
