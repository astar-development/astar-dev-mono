using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Layouts;

/// <summary>Main window layout C, "sync pairs first". Bind its DataContext to a <see cref="ViewModels.LayoutCViewModel"/>. Ctrl+K anywhere in the window focuses the command search box.</summary>
public partial class LayoutCView : UserControl
{
    private static readonly KeyGesture focusCommandSearchGesture = new(Key.K, KeyModifiers.Control);
    private TopLevel? keyboardHost;

    /// <summary>Creates the view and loads its XAML.</summary>
    public LayoutCView() => InitializeComponent();

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        keyboardHost = TopLevel.GetTopLevel(this);
        keyboardHost?.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        keyboardHost?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        keyboardHost = null;
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (!focusCommandSearchGesture.Matches(e))
            return;

        e.Handled = this.Get<TextBox>("CommandSearchBox").Focus();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
