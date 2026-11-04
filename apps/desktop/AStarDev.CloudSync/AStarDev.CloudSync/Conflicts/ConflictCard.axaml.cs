using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Conflicts;

/// <summary>Shared control used by every main window layout. Bind its DataContext to the matching view model.</summary>
public partial class ConflictCard : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public ConflictCard() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
