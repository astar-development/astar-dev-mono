using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Classification;

/// <summary>The classification page: a header with the reclassify button above the term list and suggestions tabs. Bind its DataContext to a <c>ClassificationPageViewModel</c>.</summary>
public partial class ClassificationPageContent : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public ClassificationPageContent() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
