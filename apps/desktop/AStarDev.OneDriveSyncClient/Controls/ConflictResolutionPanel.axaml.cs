using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ConflictItemViewModel = AStarDev.OneDriveSyncClient.Conflicts.ConflictItemViewModel;

namespace AStarDev.OneDriveSyncClient.Controls;

[ExcludeFromCodeCoverage]
public partial class ConflictResolutionPanel : UserControl
{
    public ConflictResolutionPanel() => InitializeComponent();

    private void OnPolicyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ConflictPolicy policy } && DataContext is ConflictItemViewModel vm)
        {
            vm.SelectedPolicy = policy;
        }
    }
}
