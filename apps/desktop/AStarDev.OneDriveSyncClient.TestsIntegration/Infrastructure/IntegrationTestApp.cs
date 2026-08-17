using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;

[assembly: AvaloniaTestApplication(typeof(AStarDev.OneDriveSyncClient.TestsIntegration.Infrastructure.IntegrationTestApp))]

namespace AStarDev.OneDriveSyncClient.TestsIntegration.Infrastructure;

public sealed class IntegrationTestApp : Application
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<IntegrationTestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}
