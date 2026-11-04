using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(AStarDev.CloudSync.TestsUnit.TestApp))]

namespace AStarDev.CloudSync.TestsUnit;

public sealed class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize() => Styles.Add(new FluentTheme());
}
