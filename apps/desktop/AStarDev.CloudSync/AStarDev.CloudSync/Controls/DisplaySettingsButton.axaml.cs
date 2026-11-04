using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Controls;

/// <summary>The settings button every layout shows: opens the display settings of the main window it sits in.</summary>
public partial class DisplaySettingsButton : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public DisplaySettingsButton() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
