using AStarDev.CloudSync.Core.DisplaySettings;
using AStarDev.CloudSync.Domain.DisplaySettings;

namespace AStarDev.CloudSync.Domain.TestsUnit.DisplaySettings;

public class GivenAnAppTheme
{
    [Theory]
    [InlineData("Light", typeof(AppTheme.Light))]
    [InlineData(" light ", typeof(AppTheme.Light))]
    [InlineData("Dark", typeof(AppTheme.Dark))]
    [InlineData(null, typeof(AppTheme.Dark))]
    [InlineData("Purple", typeof(AppTheme.Dark))]
    public void when_created_from_a_key_then_light_is_recognised_and_anything_else_is_dark(string? key, Type expected)
    {
        var theme = AppThemeFactory.FromKey(key);

        theme.ShouldBeOfType(expected);
    }

    [Fact]
    public void when_the_themes_are_converted_to_keys_then_the_keys_read_back_as_the_same_themes()
    {
        AppThemeFactory.FromKey(AppThemeFactory.CreateLight().ToKey()).ShouldBeOfType<AppTheme.Light>();
        AppThemeFactory.FromKey(AppThemeFactory.CreateDark().ToKey()).ShouldBeOfType<AppTheme.Dark>();
    }

    [Fact]
    public void when_the_default_display_preferences_are_created_then_they_are_layout_a_and_dark()
    {
        var preferences = DisplayPreferencesFactory.CreateDefault();

        preferences.Layout.ShouldBeOfType<MainLayout.UnifiedAccounts>();
        preferences.Theme.ShouldBeOfType<AppTheme.Dark>();
    }
}
