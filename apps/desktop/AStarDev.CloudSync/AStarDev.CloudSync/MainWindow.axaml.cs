using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync;

/// <summary>The CloudSync main window. Content is bound from <see cref="ViewModels.MainWindowViewModel"/>.</summary>
public partial class MainWindow : Window
{
    /// <summary>Creates the window and loads its XAML.</summary>
    public MainWindow() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
