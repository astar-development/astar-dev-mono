using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Activity;

/// <summary>The full activity list: heading, then the entries newest first with a message when there are none or they could not be read. Bind its DataContext to an <c>ActivityListViewModel</c>.</summary>
public partial class ActivityListContent : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public ActivityListContent() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
