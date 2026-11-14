using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.ViewModels.Conflicts;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Conflicts;

/// <summary>The conflict list screen. Bind its DataContext to <c>ConflictListViewModel</c>. As a modal overlay it takes focus when it is shown or opened, closes on Escape, keeps Tab inside itself and returns focus to its opener.</summary>
public partial class ConflictListView : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public ConflictListView()
    {
        InitializeComponent();
        OverlayBehavior<ConflictListViewModel>.Attach(this, list => list.IsOpen, list => list.CloseCommand, "FilterBox");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
