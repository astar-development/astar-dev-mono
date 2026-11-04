using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(AStarDev.CloudSync.TestsUnit.TestApp))]

namespace AStarDev.CloudSync.TestsUnit;

public sealed class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .WithInterFont()
        .With(new FontManagerOptions { DefaultFamilyName = "fonts:Inter#Inter" });

    public override void Initialize() => Styles.Add(new FluentTheme());
}
