using AStarDev.CloudSync.Domain.DisplaySettings;
using AStarDev.CloudSync.Theming;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;

namespace AStarDev.CloudSync.TestsUnit.Theming;

public class GivenAnAvaloniaThemeApplier
{
    [AvaloniaTheory]
    [InlineData("Light", "Light")]
    [InlineData("Dark", "Dark")]
    public void when_a_theme_is_applied_then_the_application_theme_variant_follows_it(string themeKey, string expectedVariant)
    {
        var application = Application.Current.ShouldNotBeNull();
        var original = application.RequestedThemeVariant;

        new AvaloniaThemeApplier().Apply(AppThemeFactory.FromKey(themeKey));
        var applied = application.RequestedThemeVariant;
        application.RequestedThemeVariant = original;

        applied.ShouldBe(expectedVariant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark);
    }
}
