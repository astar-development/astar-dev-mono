using AStar.Dev.OneDriveSync.old.Localisation;

namespace AStar.Dev.OneDriveSync.old.Tests.Unit.Localisation;

[TestSubject(typeof(LocalizeExtension))]
public class LocalizeExtensionShould
{
    [Fact]
    public void ReturnLocalisedString_ForKnownKey()
    {
        var sut = new LocalizeExtension("MainWindow_Title");

        var result = sut.ProvideValue(null!);

        result.ShouldBe("AStar Dev OneDrive Sync");
    }

    [Fact]
    public void ReturnKey_WhenKeyNotFound()
    {
        var sut = new LocalizeExtension("NoSuchKey_XYZ");

        var result = sut.ProvideValue(null!);

        result.ShouldBe("NoSuchKey_XYZ");
    }

    [Fact]
    public void ExposeKey_AsProperty()
        => new LocalizeExtension("Settings_ThemeLabel").Key.ShouldBe("Settings_ThemeLabel");

    [Theory]
    [InlineData("Common_Back",    "Back")]
    [InlineData("Common_Cancel",  "Cancel")]
    [InlineData("Common_Save",    "Save")]
    public void ReturnCorrectEnGbString_ForCommonKeys(string key, string expected)
        => new LocalizeExtension(key).ProvideValue(null!).ShouldBe(expected);

    [Theory]
    [InlineData("Settings_ThemeLight",  "Light")]
    [InlineData("Settings_ThemeDark",   "Dark")]
    [InlineData("Settings_ThemeSystem", "System")]
    public void ReturnCorrectEnGbString_ForThemeOptionKeys(string key, string expected)
        => new LocalizeExtension(key).ProvideValue(null!).ShouldBe(expected);
}
