using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Layouts;

/// <summary>Main window layout B, grouped by provider. Bind its DataContext to a <see cref="ViewModels.LayoutBViewModel"/>.</summary>
public partial class LayoutBView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public LayoutBView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
