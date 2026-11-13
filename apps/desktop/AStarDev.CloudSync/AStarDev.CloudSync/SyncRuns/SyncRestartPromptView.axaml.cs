using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.SyncRuns;

/// <summary>The question shown over the main window when Sync Now is pressed while a sync is already running (SE-08). Bind its DataContext to a <see cref="ViewModels.SyncRuns.SyncRestartPromptViewModel"/>.</summary>
public partial class SyncRestartPromptView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public SyncRestartPromptView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
