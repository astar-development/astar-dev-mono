using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Overview;

/// <summary>Shared control used by every main window layout. Bind its DataContext to the matching view model.</summary>
public partial class StatTile : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public StatTile() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
