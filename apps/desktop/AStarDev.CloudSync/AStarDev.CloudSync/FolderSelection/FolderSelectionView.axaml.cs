using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.FolderSelection;

/// <summary>The folder selection dialog panel shown over the main window. Bind its DataContext to a <see cref="ViewModels.FolderSelection.FolderSelectionViewModel"/>.</summary>
public partial class FolderSelectionView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public FolderSelectionView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
