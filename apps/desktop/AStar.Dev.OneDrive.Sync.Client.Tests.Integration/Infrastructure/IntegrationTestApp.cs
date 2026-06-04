using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;

[assembly: AvaloniaTestApplication(typeof(AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure.IntegrationTestApp))]

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;

public sealed class IntegrationTestApp : Application
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<IntegrationTestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
