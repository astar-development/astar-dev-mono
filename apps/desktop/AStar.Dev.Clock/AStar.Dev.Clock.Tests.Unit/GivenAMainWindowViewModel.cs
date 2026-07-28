using AStar.Dev.Clock.Theming;

namespace AStar.Dev.Clock.Tests.Unit;

public sealed class GivenAMainWindowViewModel
{
    private static MainWindowViewModel CreateSut() => new(Substitute.For<IThemeService>());

    [Fact]
    public void when_created_then_second_hand_is_shown() => CreateSut().ShowSecondHand.ShouldBeTrue();

    [Fact]
    public void when_created_then_toggle_second_hand_text_is_hide_second_hand() => CreateSut().ToggleSecondHandText.ShouldBe("Hide Second-hand");

    [Fact]
    public void when_toggle_second_hand_command_is_executed_then_second_hand_is_hidden()
    {
        var sut = CreateSut();

        sut.ToggleSecondHandCommand.Execute(null);

        sut.ShowSecondHand.ShouldBeFalse();
    }

    [Fact]
    public void when_toggle_second_hand_command_is_executed_then_toggle_second_hand_text_is_show_second_hand()
    {
        var sut = CreateSut();

        sut.ToggleSecondHandCommand.Execute(null);

        sut.ToggleSecondHandText.ShouldBe("Show Second-hand");
    }

    [Fact]
    public void when_toggle_second_hand_command_is_executed_twice_then_second_hand_is_shown_again()
    {
        var sut = CreateSut();

        sut.ToggleSecondHandCommand.Execute(null);
        sut.ToggleSecondHandCommand.Execute(null);

        sut.ShowSecondHand.ShouldBeTrue();
    }
}
