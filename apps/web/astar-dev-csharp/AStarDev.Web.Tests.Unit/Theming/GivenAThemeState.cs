using AStarDev.Web.Theming;
using Blazored.LocalStorage;
using NSubstitute;

namespace AStarDev.Web.Tests.Unit.Theming;

public class GivenAThemeState
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();

    [Fact]
    public void when_constructed_then_current_theme_is_dark()
    {
        var sut = new ThemeState(localStorage);

        sut.Current.ShouldBe(Theme.Dark);
    }

    [Fact]
    public async Task when_initialized_and_no_theme_is_stored_then_current_theme_stays_dark()
    {
        localStorage.GetItemAsStringAsync("theme", Arg.Any<CancellationToken>()).Returns((string?)null);
        var sut = new ThemeState(localStorage);

        await sut.InitializeAsync();

        sut.Current.ShouldBe(Theme.Dark);
    }

    [Fact]
    public async Task when_initialized_and_a_valid_theme_is_stored_then_current_theme_is_the_stored_theme()
    {
        localStorage.GetItemAsStringAsync("theme", Arg.Any<CancellationToken>()).Returns("metal");
        var sut = new ThemeState(localStorage);

        await sut.InitializeAsync();

        sut.Current.ShouldBe(Theme.Metal);
    }

    [Fact]
    public async Task when_initialized_and_the_stored_theme_is_invalid_then_current_theme_stays_dark()
    {
        localStorage.GetItemAsStringAsync("theme", Arg.Any<CancellationToken>()).Returns("not-a-theme");
        var sut = new ThemeState(localStorage);

        await sut.InitializeAsync();

        sut.Current.ShouldBe(Theme.Dark);
    }

    [Fact]
    public async Task when_theme_is_set_then_current_theme_updates()
    {
        var sut = new ThemeState(localStorage);

        await sut.SetThemeAsync(Theme.Polished);

        sut.Current.ShouldBe(Theme.Polished);
    }

    [Fact]
    public async Task when_theme_is_set_then_the_theme_is_persisted_to_local_storage()
    {
        var sut = new ThemeState(localStorage);

        await sut.SetThemeAsync(Theme.Light);

        await localStorage.Received(1).SetItemAsStringAsync("theme", "light", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_theme_is_set_then_on_change_is_raised()
    {
        var sut = new ThemeState(localStorage);
        bool raised = false;
        sut.OnChange += () => raised = true;

        await sut.SetThemeAsync(Theme.Metal);

        raised.ShouldBeTrue();
    }
}
