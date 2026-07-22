using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.ReactiveUI;
using Velopack;

namespace AStar.Dev.Wallpaper.Scraper;

[ExcludeFromCodeCoverage]
internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack hooks install/update/uninstall events and may exit the process;
        // it must run before anything else touches the app state.
        VelopackApp.Build().Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
