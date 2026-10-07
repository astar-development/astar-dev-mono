using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Activity;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.OneDriveSyncClient.Controls;

[ExcludeFromCodeCoverage]
public partial class GlobalConflictResolutionPanel : UserControl
{
    public GlobalConflictResolutionPanel() => InitializeComponent();

    private void OnPolicyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ConflictPolicy policy } && DataContext is ActivityViewModel vm)
        {
            vm.SelectGlobalPolicyCommand.Execute(policy);
        }
    }
}
