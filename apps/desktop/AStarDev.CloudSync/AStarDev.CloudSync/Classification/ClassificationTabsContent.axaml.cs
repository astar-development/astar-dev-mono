using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Classification;

/// <summary>The classification view: a term list tab and a suggestions tab. Bind its DataContext to a <c>ClassificationTabsViewModel</c>.</summary>
public partial class ClassificationTabsContent : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public ClassificationTabsContent() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
