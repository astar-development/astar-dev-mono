using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AStar.Dev.OneDriveSync.Infrastructure.Shell;

public partial class IconRailButton : UserControl
{
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<IconRailButton, bool>(nameof(IsActive));

    public static readonly StyledProperty<string> IconPathProperty =
        AvaloniaProperty.Register<IconRailButton, string>(nameof(IconPath), string.Empty);

    public static readonly StyledProperty<string> TooltipLabelProperty =
        AvaloniaProperty.Register<IconRailButton, string>(nameof(TooltipLabel), string.Empty);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<IconRailButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<IconRailButton, object?>(nameof(CommandParameter));

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public string IconPath
    {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public string TooltipLabel
    {
        get => GetValue(TooltipLabelProperty);
        set => SetValue(TooltipLabelProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IconRailButton()
    {
        InitializeComponent();
        _ = IsActiveProperty.Changed.AddClassHandler<IconRailButton>(OnIsActiveChanged);
        _ = IconPathProperty.Changed.AddClassHandler<IconRailButton>(OnIconPathChanged);
        _ = TooltipLabelProperty.Changed.AddClassHandler<IconRailButton>(OnTooltipChanged);
    }

    private static void OnIsActiveChanged(IconRailButton sender, AvaloniaPropertyChangedEventArgs e)
    {
        var active = e.GetNewValue<bool>();
        sender.ActiveBar.IsVisible = active;

        if (active)
            sender.RailBtn.Classes.Add("active");
        else
            _ = sender.RailBtn.Classes.Remove("active");
    }

    private static void OnIconPathChanged(IconRailButton sender, AvaloniaPropertyChangedEventArgs e)
    {
        var path = e.GetNewValue<string>();
        sender.Icon.Data = string.IsNullOrEmpty(path) ? null : Geometry.Parse(path);
    }

    private static void OnTooltipChanged(IconRailButton sender, AvaloniaPropertyChangedEventArgs e)
        => sender.TooltipText.Text = e.GetNewValue<string>();

    private void RailBtnClick(object? sender, RoutedEventArgs e)
    {
        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);
    }
}
