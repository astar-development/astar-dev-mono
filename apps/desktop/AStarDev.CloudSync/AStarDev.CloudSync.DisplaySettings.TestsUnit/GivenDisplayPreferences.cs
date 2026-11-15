using AStarDev.CloudSync.DisplaySettings;

namespace AStarDev.CloudSync.DisplaySettings.TestsUnit;

public class GivenDisplayPreferences
{
    [Theory]
    [InlineData("en-GB", "en-GB")]
    [InlineData("EN-gb", "en-GB")]
    [InlineData("en-gb", "en-GB")]
    [InlineData("xx-YY", "en-GB")]
    [InlineData("de-DE", "en-GB")]
    [InlineData("", "en-GB")]
    [InlineData("   ", "en-GB")]
    [InlineData(null, "en-GB")]
    public void when_a_locale_is_given_then_it_is_normalised_to_a_supported_one(string? locale, string expected)
    {
        DisplayPreferencesFactory.Create(MainLayoutFactory.CreateUnifiedAccounts(), AppThemeFactory.CreateDark(), locale).Locale.ShouldBe(expected);
    }

    [Fact]
    public void when_no_locale_is_given_then_the_default_is_en_gb()
    {
        DisplayPreferencesFactory.Create(MainLayoutFactory.CreateUnifiedAccounts(), AppThemeFactory.CreateDark()).Locale.ShouldBe("en-GB");
    }
}
