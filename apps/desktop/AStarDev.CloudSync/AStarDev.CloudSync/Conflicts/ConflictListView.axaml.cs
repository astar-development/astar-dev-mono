using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Conflicts;

/// <summary>The conflict list screen. Bind its DataContext to <c>ConflictListViewModel</c>.</summary>
public partial class ConflictListView : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public ConflictListView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
