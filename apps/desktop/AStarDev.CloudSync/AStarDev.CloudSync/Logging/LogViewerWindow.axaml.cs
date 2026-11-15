using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Logging;

/// <summary>The hidden log viewer window (#1411). Its content is bound from <see cref="ViewModels.Logging.LogViewerViewModel"/>.</summary>
[ExcludeFromCodeCoverage(Justification = "A window that only loads its XAML; the logic is in LogViewerViewModel.")]
public partial class LogViewerWindow : Window
{
    /// <summary>Creates the window and loads its XAML.</summary>
    public LogViewerWindow() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
