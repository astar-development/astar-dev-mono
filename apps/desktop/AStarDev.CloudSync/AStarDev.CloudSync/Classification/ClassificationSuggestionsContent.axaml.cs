using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Classification;

/// <summary>The classification window's suggestions list. Bind its DataContext to a <c>ClassificationSuggestionsViewModel</c>.</summary>
public partial class ClassificationSuggestionsContent : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public ClassificationSuggestionsContent() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
