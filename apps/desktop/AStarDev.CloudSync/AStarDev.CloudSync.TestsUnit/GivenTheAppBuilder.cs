using System.Reflection;
using Avalonia;
using Avalonia.Media;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenTheAppBuilder
{
    private const string InterFamilyName = "fonts:Inter#Inter";

    [Fact]
    public void when_the_real_app_builder_is_configured_then_inter_is_the_default_font()
    {
        var builder = BuildRealAppBuilder();

        string defaultFamilyName = ConfiguredDefaultFamilyName(builder);

        defaultFamilyName.ShouldBe(InterFamilyName);
    }

    [Fact]
    public void when_the_test_app_builder_is_configured_then_it_uses_the_same_default_font_as_the_real_app()
    {
        string realAppFamilyName = ConfiguredDefaultFamilyName(BuildRealAppBuilder());

        string testAppFamilyName = ConfiguredDefaultFamilyName(TestApp.BuildAvaloniaApp());

        testAppFamilyName.ShouldBe(realAppFamilyName);
    }

    private static AppBuilder BuildRealAppBuilder()
    {
        var program = typeof(App).Assembly.GetType("AStarDev.CloudSync.Program", throwOnError: true).ShouldNotBeNull();
        var buildAvaloniaApp = program.GetMethod("BuildAvaloniaApp", BindingFlags.Public | BindingFlags.Static).ShouldNotBeNull();

        return buildAvaloniaApp.Invoke(null, null).ShouldBeOfType<AppBuilder>();
    }

    private static string ConfiguredDefaultFamilyName(AppBuilder builder)
    {
        var optionsInitializersField = typeof(AppBuilder).GetField("_optionsInitializers", BindingFlags.NonPublic | BindingFlags.Instance).ShouldNotBeNull();
        var optionsInitializers = optionsInitializersField.GetValue(builder) as Delegate;

        return (optionsInitializers?.GetInvocationList() ?? [])
            .Select(initializer => initializer.Target)
            .OfType<object>()
            .SelectMany(closure => closure.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Select(field => field.GetValue(closure)))
            .OfType<FontManagerOptions>()
            .Select(options => options.DefaultFamilyName ?? string.Empty)
            .LastOrDefault(string.Empty);
    }
}
