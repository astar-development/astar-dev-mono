using System.Windows.Input;
using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.FolderSelection;

/// <summary>The folder selection dialog panel shown over the main window. Bind its DataContext to a <see cref="ViewModels.FolderSelection.FolderSelectionViewModel"/>. As a modal overlay it takes focus when opened, closes on Escape, keeps Tab inside itself and returns focus to its opener.</summary>
public partial class FolderSelectionView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public FolderSelectionView()
    {
        InitializeComponent();
        OverlayBehavior<FolderSelectionViewModel>.Attach(this, dialog => dialog.IsOpen, DismissCommand);
    }

    private static ICommand DismissCommand(FolderSelectionViewModel dialog) => dialog.IsConfirming ? dialog.BackCommand : dialog.CancelCommand;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
