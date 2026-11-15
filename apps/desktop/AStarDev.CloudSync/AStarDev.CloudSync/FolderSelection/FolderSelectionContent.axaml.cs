using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.FolderSelection;

/// <summary>The folder selection itself: account list, folder tree, confirmation and the Cancel, Apply, Back and confirm buttons. Bind its DataContext to a <c>FolderSelectionViewModel</c>. The modal <see cref="FolderSelectionView"/> wraps it in a dialog panel; a page hosts it directly.</summary>
public partial class FolderSelectionContent : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public FolderSelectionContent() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
