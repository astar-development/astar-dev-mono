using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;

[assembly: AvaloniaTestApplication(typeof(AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Controls.TestApp))]

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Controls;

public sealed class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
