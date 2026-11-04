using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(AStarDev.CloudSync.TestsUnit.TestApp))]

namespace AStarDev.CloudSync.TestsUnit;

public sealed class TestApp : Application
{
    private static readonly Uri appBaseUri = new("avares://AStarDev.CloudSync/");

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .WithInterFont()
        .With(new FontManagerOptions { DefaultFamilyName = "fonts:Inter#Inter" });

    public override void Initialize()
    {
        Resources.MergedDictionaries.Add(new ResourceInclude(appBaseUri) { Source = new Uri("avares://AStarDev.CloudSync/Themes/Tokens.axaml") });
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(appBaseUri) { Source = new Uri("avares://AStarDev.CloudSync/Themes/ControlStyles.axaml") });
    }
}
