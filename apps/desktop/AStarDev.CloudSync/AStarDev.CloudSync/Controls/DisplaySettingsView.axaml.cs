using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Controls;

/// <summary>The layout and theme choices. Bind its DataContext to a DisplaySettingsViewModel.</summary>
public partial class DisplaySettingsView : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public DisplaySettingsView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
