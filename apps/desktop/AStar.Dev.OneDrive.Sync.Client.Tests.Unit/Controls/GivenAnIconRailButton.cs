using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using AStar.Dev.OneDrive.Sync.Client.Controls;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Controls;

public sealed class GivenAnIconRailButton
{
    private const string ActiveClassName = "active";

    [Fact]
    public void when_is_active_property_is_inspected_then_name_is_IsActive() =>
        IconRailButton.IsActiveProperty.Name.ShouldBe(nameof(IconRailButton.IsActive));

    [Fact]
    public void when_tooltip_label_property_is_inspected_then_default_value_is_empty_string() =>
        IconRailButton.TooltipLabelProperty.GetDefaultValue(typeof(IconRailButton)).ShouldBe(string.Empty);

    [Fact]
    public void when_icon_data_property_is_inspected_then_default_value_is_null() =>
        IconRailButton.IconDataProperty.GetDefaultValue(typeof(IconRailButton)).ShouldBeNull();

    [Fact]
    public void when_command_property_is_inspected_then_default_value_is_null() =>
        IconRailButton.CommandProperty.GetDefaultValue(typeof(IconRailButton)).ShouldBeNull();

    [Fact]
    public void when_command_parameter_property_is_inspected_then_default_value_is_null() =>
        IconRailButton.CommandParameterProperty.GetDefaultValue(typeof(IconRailButton)).ShouldBeNull();

    [AvaloniaFact]
    public void when_is_active_set_to_true_then_active_bar_becomes_visible()
    {
        var sut = new IconRailButton();

        sut.IsActive = true;

        sut.FindControl<Rectangle>("ActiveBar")!.IsVisible.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void when_is_active_set_to_true_then_rail_button_has_active_class()
    {
        var sut = new IconRailButton();

        sut.IsActive = true;

        sut.FindControl<Button>("RailBtn")!.Classes.Contains(ActiveClassName).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void when_is_active_set_to_false_after_true_then_active_bar_becomes_hidden()
    {
        var sut = new IconRailButton();
        sut.IsActive = true;

        sut.IsActive = false;

        sut.FindControl<Rectangle>("ActiveBar")!.IsVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void when_is_active_set_to_false_after_true_then_rail_button_loses_active_class()
    {
        var sut = new IconRailButton();
        sut.IsActive = true;

        sut.IsActive = false;

        sut.FindControl<Button>("RailBtn")!.Classes.Contains(ActiveClassName).ShouldBeFalse();
    }

    [AvaloniaFact]
    public void when_tooltip_label_is_set_then_tooltip_text_block_shows_label()
    {
        var sut = new IconRailButton();
        const string label = "My Tooltip";

        sut.TooltipLabel = label;

        sut.FindControl<TextBlock>("TooltipText")!.Text.ShouldBe(label);
    }

    [AvaloniaFact]
    public void when_icon_data_is_set_then_path_icon_data_matches()
    {
        var sut = new IconRailButton();
        var geometry = Geometry.Parse("M 0 0 L 10 10");

        sut.IconData = geometry;

        sut.FindControl<PathIcon>("Icon")!.Data.ShouldBe(geometry);
    }

    [AvaloniaFact]
    public void when_rail_btn_clicked_and_command_can_execute_then_command_execute_is_called()
    {
        var command = Substitute.For<ICommand>();
        command.CanExecute(Arg.Any<object?>()).Returns(true);
        var sut = new IconRailButton { Command = command };
        var railBtn = sut.FindControl<Button>("RailBtn")!;

        railBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        command.Received(1).Execute(sut.CommandParameter);
    }

    [AvaloniaFact]
    public void when_rail_btn_clicked_and_command_cannot_execute_then_command_execute_is_not_called()
    {
        var command = Substitute.For<ICommand>();
        command.CanExecute(Arg.Any<object?>()).Returns(false);
        var sut = new IconRailButton { Command = command };
        var railBtn = sut.FindControl<Button>("RailBtn")!;

        railBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        command.DidNotReceive().Execute(Arg.Any<object?>());
    }

    [AvaloniaFact]
    public void when_rail_btn_clicked_and_command_is_null_then_no_exception_is_thrown()
    {
        var sut = new IconRailButton { Command = null };
        var railBtn = sut.FindControl<Button>("RailBtn")!;

        Should.NotThrow(() => railBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
    }
}
