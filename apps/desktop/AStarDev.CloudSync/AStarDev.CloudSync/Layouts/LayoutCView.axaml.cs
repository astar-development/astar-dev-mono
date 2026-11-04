using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Layouts;

/// <summary>Main window layout C, "sync pairs first". Bind its DataContext to a <see cref="ViewModels.LayoutCViewModel"/>.</summary>
public partial class LayoutCView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public LayoutCView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
