using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.SyncRestart;

/// <summary>The SE-08 question panel shown over the main window. Bind its DataContext to a <see cref="ViewModels.SyncRestart.ConfirmSyncRestartViewModel"/>.</summary>
public partial class ConfirmSyncRestartView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public ConfirmSyncRestartView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
