using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.FolderSelection;

/// <summary>One folder of the selection tree, with its children nested below it. Bind its DataContext to a <see cref="ViewModels.FolderSelection.FolderNodeViewModel"/>.</summary>
public partial class FolderNodeView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public FolderNodeView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
