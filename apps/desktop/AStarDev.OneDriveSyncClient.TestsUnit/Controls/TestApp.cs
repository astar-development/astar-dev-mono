using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;

[assembly: AvaloniaTestApplication(typeof(AStarDev.OneDriveSyncClient.TestsUnit.Controls.TestApp))]

namespace AStarDev.OneDriveSyncClient.TestsUnit.Controls;

public sealed class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
