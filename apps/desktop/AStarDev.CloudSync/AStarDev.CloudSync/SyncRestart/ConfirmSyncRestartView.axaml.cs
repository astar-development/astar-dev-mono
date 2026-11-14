using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.ViewModels.SyncRestart;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.SyncRestart;

/// <summary>The SE-08 question panel shown over the main window. Bind its DataContext to a <see cref="ViewModels.SyncRestart.ConfirmSyncRestartViewModel"/>. As a modal overlay it takes focus when opened, treats Escape as keeping the current sync, keeps Tab inside itself and returns focus to its opener.</summary>
public partial class ConfirmSyncRestartView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public ConfirmSyncRestartView()
    {
        InitializeComponent();
        OverlayBehavior<ConfirmSyncRestartViewModel>.Attach(this, dialog => dialog.IsOpen, dialog => dialog.KeepCommand);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
