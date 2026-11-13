using AStarDev.CloudSync.ViewModels.SyncRuns;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync;

/// <summary>The CloudSync main window. Content is bound from <see cref="ViewModels.MainWindowViewModel"/>.</summary>
public partial class MainWindow : Window
{
    /// <summary>Creates the window and loads its XAML.</summary>
    public MainWindow() => InitializeComponent();

    /// <summary>Gives the SE-08 question overlay its view model. The question is hidden until the view model asks.</summary>
    /// <param name="prompt">The question's view model.</param>
    public void AttachSyncRestartPrompt(SyncRestartPromptViewModel prompt) => this.GetControl<Border>("SyncRestartPromptHost").DataContext = prompt;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
