using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Layouts;

/// <summary>Main window layout A, "unified account list". Bind its DataContext to <see cref="ViewModels.LayoutAViewModel"/>.</summary>
public partial class LayoutAView : UserControl
{
    /// <summary>Creates the layout and loads its XAML.</summary>
    public LayoutAView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
