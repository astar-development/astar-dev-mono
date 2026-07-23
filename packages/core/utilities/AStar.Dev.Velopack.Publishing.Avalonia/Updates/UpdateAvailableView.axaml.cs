using global::Avalonia.Controls;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>The shared update-available dialog window, showing title/version/release notes and a Restart-now/Later choice.</summary>
public partial class UpdateAvailableView : Window
{
    /// <summary>Loads the window's XAML content.</summary>
    public UpdateAvailableView() => InitializeComponent();
}
