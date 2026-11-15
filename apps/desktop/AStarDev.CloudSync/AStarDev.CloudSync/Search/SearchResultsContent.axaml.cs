using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Search;

/// <summary>The search page: a search box over the accounts and folders it matches. Bind its DataContext to a <c>SearchResultsViewModel</c>.</summary>
public partial class SearchResultsContent : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public SearchResultsContent() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
