using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Common;

/// <summary>Shared control used by every main window layout. Bind its DataContext to the matching view model.</summary>
public partial class StatusPill : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public StatusPill() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
