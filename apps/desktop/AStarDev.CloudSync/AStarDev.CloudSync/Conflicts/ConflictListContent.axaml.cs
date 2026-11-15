using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Conflicts;

/// <summary>The conflict list itself: title, kind filter, refresh and the grouped conflicts and held deletions. Bind its DataContext to <c>ConflictListViewModel</c>. The modal <see cref="ConflictListView"/> wraps it with a Close button; a page sets <see cref="IsEmbedded"/> to hide that button.</summary>
public partial class ConflictListContent : UserControl
{
    /// <summary>Whether the list is a page rather than a dialog, so it has no Close button.</summary>
    public static readonly StyledProperty<bool> IsEmbeddedProperty = AvaloniaProperty.Register<ConflictListContent, bool>(nameof(IsEmbedded));

    /// <summary>Creates the control and loads its XAML.</summary>
    public ConflictListContent() => InitializeComponent();

    /// <summary>Whether the list is a page rather than a dialog, so it has no Close button.</summary>
    public bool IsEmbedded
    {
        get => GetValue(IsEmbeddedProperty);
        set => SetValue(IsEmbeddedProperty, value);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
