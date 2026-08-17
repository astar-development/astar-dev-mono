using AStar.Dev.Clock.Theming;

namespace AStar.Dev.Clock.TestsUnit;

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

    [Fact]
    public void when_select_theme_command_is_executed_with_dark_then_the_theme_service_applies_the_dark_theme()
    {
        var themeService = Substitute.For<IThemeService>();
        var sut = new MainWindowViewModel(themeService);

        sut.SelectThemeCommand.Execute(ThemeMode.Dark);

        themeService.Received(1).ApplyTheme(ThemeMode.Dark);
    }

    [Fact]
    public void when_select_theme_command_is_executed_with_dark_then_is_dark_theme_selected_is_true()
    {
        var sut = CreateSut();

        sut.SelectThemeCommand.Execute(ThemeMode.Dark);

        sut.IsDarkThemeSelected.ShouldBeTrue();
    }

    [Fact]
    public void when_select_theme_command_is_executed_with_dark_then_is_system_theme_selected_is_false()
    {
        var sut = CreateSut();

        sut.SelectThemeCommand.Execute(ThemeMode.Dark);

        sut.IsSystemThemeSelected.ShouldBeFalse();
    }

    [Fact]
    public void when_select_theme_command_is_executed_with_dark_then_is_light_theme_selected_is_false()
    {
        var sut = CreateSut();

        sut.SelectThemeCommand.Execute(ThemeMode.Dark);

        sut.IsLightThemeSelected.ShouldBeFalse();
    }

    [Fact]
    public void when_constructed_with_light_theme_as_the_current_theme_then_is_light_theme_selected_is_true()
    {
        var themeService = Substitute.For<IThemeService>();
        themeService.CurrentTheme.Returns(ThemeMode.Light);

        var sut = new MainWindowViewModel(themeService);

        sut.IsLightThemeSelected.ShouldBeTrue();
    }
}
