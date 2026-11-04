using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(AStarDev.CloudSync.TestsUnit.TestApp))]

namespace AStarDev.CloudSync.TestsUnit;

public sealed class TestApp : Application
{
    // typeof(App) loads the app assembly so avares:// can resolve its precompiled XAML when one test class runs alone.
    private static readonly Uri appBaseUri = new($"avares://{typeof(App).Assembly.GetName().Name}/");

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .WithCloudSyncFonts();

    public override void Initialize()
    {
        Resources.MergedDictionaries.Add(new ResourceInclude(appBaseUri) { Source = new Uri("avares://AStarDev.CloudSync/Themes/Tokens.axaml") });
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(appBaseUri) { Source = new Uri("avares://AStarDev.CloudSync/Themes/ControlStyles.axaml") });
    }
}
