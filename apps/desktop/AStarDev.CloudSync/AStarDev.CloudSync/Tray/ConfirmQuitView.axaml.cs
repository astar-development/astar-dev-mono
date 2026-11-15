using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.ViewModels.Tray;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Tray;

/// <summary>The question panel shown over the main window when Quit is chosen while a sync is running (#1409). Bind its DataContext to a <see cref="ConfirmQuitViewModel"/>. As a modal overlay it takes focus when opened, treats Escape as keeping the app open, keeps Tab inside itself and returns focus to its opener.</summary>
public partial class ConfirmQuitView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public ConfirmQuitView()
    {
        InitializeComponent();
        OverlayBehavior<ConfirmQuitViewModel>.Attach(this, dialog => dialog.IsOpen, dialog => dialog.StayCommand);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
