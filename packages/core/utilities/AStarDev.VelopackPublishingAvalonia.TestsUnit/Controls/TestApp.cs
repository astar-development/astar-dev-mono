using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;

[assembly: AvaloniaTestApplication(typeof(AStar.Dev.Velopack.Publishing.Avalonia.TestsUnit.Controls.TestApp))]

namespace AStar.Dev.Velopack.Publishing.Avalonia.TestsUnit.Controls;

internal sealed class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
