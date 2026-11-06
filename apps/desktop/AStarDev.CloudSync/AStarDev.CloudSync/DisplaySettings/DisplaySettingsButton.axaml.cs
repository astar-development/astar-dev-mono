using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AStarDev.CloudSync.ViewModels.DisplaySettings;

namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>The settings button every layout shows: opens the display settings of the main window it sits in.</summary>
public partial class DisplaySettingsButton : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public DisplaySettingsButton() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnSettingsOpening(object? sender, EventArgs e)
    {
        if (DataContext is DisplaySettingsViewModel settings)
            settings.PlaintextCacheConsent.Refresh();
    }
}
