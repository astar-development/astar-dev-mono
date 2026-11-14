using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Conflicts;

/// <summary>One conflict of the conflict list with its strategy buttons. Bind its DataContext to <c>ConflictItemViewModel</c>.</summary>
public partial class ConflictCard : UserControl
{
    /// <summary>Creates the control and loads its XAML.</summary>
    public ConflictCard() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
